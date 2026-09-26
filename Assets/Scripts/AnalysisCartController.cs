using UnityEngine;
using UnityEngine.InputSystem;

// Carrinho de analise (rover) em 3a pessoa.
// W/S acelera/re, A/D esterca, Espaco freia, Esc solta o cursor.
// A camera segue por tras/acima com suavizacao.
[RequireComponent(typeof(CharacterController))]
public class AnalysisCartController : MonoBehaviour
{
    [Header("Direcao")]
    public float maxSpeed = 9f;
    public float acceleration = 12f;
    public float turnSpeed = 90f;      // graus/seg em velocidade cheia
    public float gravity = -20f;

    [Header("Camera 3a pessoa")]
    public Transform cameraRig;        // objeto da camera (filho ou solto)
    public Vector3 cameraOffset = new Vector3(0f, 3.2f, -6.5f);
    public float cameraLerp = 8f;
    public float lookHeight = 1.2f;

    [Header("Camera: desvio de obstaculo")]
    public LayerMask cameraObstacles = ~0;
    public float cameraProbeRadius = 0.28f;
    public float cameraMinDistance = 1.7f;
    public float cameraPullInLerp = 22f;   // recolhe rapido, para nunca entrar na parede
    public float cameraPushOutLerp = 4f;   // volta devagar, para nao dar solavanco

    CharacterController cc;
    float speed;
    float velY;
    float camDistance = -1f;

    // Lidos por CartWheels; expostos em vez de duplicar a leitura de input.
    public float CurrentSpeed { get { return speed; } }
    public float SteerInput { get; private set; }

    float shake;          // amplitude atual do tranco, decai sozinha
    float shakeSeed;

    // Chamado por CartHazardResponse quando o rover entra num buraco/bueiro.
    // Fica aqui porque 'speed' e 'velY' sao estado privado do movimento: um
    // script externo mexendo neles direto criaria um segundo sistema.
    public void ApplyImpact(float speedMultiplier, float verticalKick, float shakeAmount)
    {
        speed *= Mathf.Clamp01(speedMultiplier);
        if (verticalKick > velY) velY = verticalKick;
        if (shakeAmount > shake) { shake = shakeAmount; shakeSeed = Random.value * 100f; }
    }

    void Start()
    {
        cc = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if (cameraRig != null)
        {
            cameraRig.position = transform.TransformPoint(cameraOffset);
            cameraRig.LookAt(transform.position + Vector3.up * lookHeight);
        }
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        float throttle = (kb.wKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed ? 1f : 0f);
        float steer = (kb.dKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed ? 1f : 0f);
        SteerInput = steer;
        bool brake = kb.spaceKey.isPressed;

        float target = brake ? 0f : throttle * maxSpeed;
        speed = Mathf.MoveTowards(speed, target, acceleration * Time.deltaTime);

        // so vira quando esta em movimento; re inverte o esterco
        if (Mathf.Abs(speed) > 0.15f)
            transform.Rotate(0f, steer * turnSpeed * Time.deltaTime * Mathf.Sign(speed), 0f);

        if (cc.isGrounded && velY < 0f) velY = -2f;
        velY += gravity * Time.deltaTime;

        Vector3 move = transform.forward * speed + Vector3.up * velY;
        cc.Move(move * Time.deltaTime);

        if (kb.escapeKey.wasPressedThisFrame)
        {
            bool locked = Cursor.lockState == CursorLockMode.Locked;
            Cursor.lockState = locked ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = locked;
        }
    }

    // Coloca a camera direto na posicao ideal, sem interpolar. Usado ao
    // reposicionar o rover no spawn: senao a camera viajaria do ponto antigo.
    public void SnapCamera()
    {
        camDistance = -1f;
        if (cameraRig == null) return;
        Vector3 pivot = transform.position + Vector3.up * lookHeight;
        cameraRig.position = transform.TransformPoint(cameraOffset);
        cameraRig.LookAt(pivot);
    }

    void LateUpdate()
    {
        if (cameraRig == null) return;

        Vector3 pivot = transform.position + Vector3.up * lookHeight;
        Vector3 desired = transform.TransformPoint(cameraOffset);
        Vector3 toCam = desired - pivot;
        float fullDistance = toCam.magnitude;
        if (fullDistance < 0.01f) return;
        Vector3 dir = toCam / fullDistance;

        // O cast comeca fora da capsula do proprio rover, senao o primeiro hit
        // seria sempre o CharacterController. Triggers sao ignorados de
        // proposito: buracos e bueiros sao triggers e puxariam a camera.
        float startOffset = cc.radius + cameraProbeRadius + 0.05f;
        float targetDistance = fullDistance;

        if (fullDistance > startOffset)
        {
            RaycastHit hit;
            if (Physics.SphereCast(pivot + dir * startOffset, cameraProbeRadius, dir,
                                   out hit, fullDistance - startOffset,
                                   cameraObstacles, QueryTriggerInteraction.Ignore))
                targetDistance = startOffset + hit.distance - cameraProbeRadius;
        }
        targetDistance = Mathf.Clamp(targetDistance, cameraMinDistance, fullDistance);

        if (camDistance < 0f) camDistance = targetDistance;   // primeiro frame
        // assimetrico: recolher e urgente, voltar pode ser suave
        float lerp = targetDistance < camDistance ? cameraPullInLerp : cameraPushOutLerp;
        camDistance = Mathf.Lerp(camDistance, targetDistance, 1f - Mathf.Exp(-lerp * Time.deltaTime));

        // tranco do buraco: perturbacao curta somada a posicao ja resolvida,
        // para nao interferir no calculo de distancia/obstaculo acima
        Vector3 camTarget = pivot + dir * camDistance;
        if (shake > 0.0005f)
        {
            float t = Time.time * 26f + shakeSeed;
            camTarget += new Vector3(
                (Mathf.PerlinNoise(t, 0f) - 0.5f),
                (Mathf.PerlinNoise(0f, t) - 0.5f),
                0f) * shake;
            shake = Mathf.Lerp(shake, 0f, 1f - Mathf.Exp(-9f * Time.deltaTime));
        }
        // Suaviza so quando nao ha obstaculo; com obstaculo vai direto, para a
        // camera nao atravessar a parede durante a interpolacao.
        bool blocked = targetDistance < fullDistance - 0.01f;
        cameraRig.position = blocked
            ? camTarget
            : Vector3.Lerp(cameraRig.position, camTarget, 1f - Mathf.Exp(-cameraLerp * Time.deltaTime));
        cameraRig.LookAt(pivot);
    }
}
