using UnityEngine;

// Giro e esterco visuais das rodas, dirigidos pela velocidade real do rover.
// Nao move o veiculo e nao le input: le AnalysisCartController, para nao
// existirem dois sistemas de movimentacao.
[RequireComponent(typeof(AnalysisCartController))]
public class CartWheels : MonoBehaviour
{
    [Header("Rodas (giram no proprio eixo)")]
    public Transform[] wheels;

    [Header("Pivos de esterco (somente dianteiras)")]
    public Transform steerLeft;
    public Transform steerRight;

    [Header("Parametros")]
    public float wheelRadius = 0.16f;   // cilindro escala 0.32 -> raio 0.16
    public float maxSteerAngle = 26f;
    public float steerLerp = 9f;

    AnalysisCartController ctrl;
    float steerAngle;

    void Awake()
    {
        ctrl = GetComponent<AnalysisCartController>();
    }

    void Update()
    {
        float dt = Time.deltaTime;

        // --- giro proporcional a distancia percorrida ---
        // Rolamento sem escorregar: omega = v / r, em torno do eixo do pneu.
        // As rodas vem com rotZ=90, logo o Y local aponta para -X do veiculo;
        // resolvendo omega x r_contato + v = 0 para avanco em +Z, a rotacao em
        // torno desse Y local fica NEGATIVA. Dai o sinal de menos.
        if (wheels != null && wheelRadius > 0.0001f)
        {
            float degPerSec = -(ctrl.CurrentSpeed / wheelRadius) * Mathf.Rad2Deg;
            float delta = degPerSec * dt;
            // parado = nao gira: o angulo vem da velocidade, nunca de um clock
            if (Mathf.Abs(delta) > 0.0001f)
            {
                for (int i = 0; i < wheels.Length; i++)
                {
                    if (wheels[i] == null) continue;
                    wheels[i].Rotate(0f, delta, 0f, Space.Self);
                }
            }
        }

        // --- esterco das dianteiras ---
        // So esterca com o veiculo em movimento, igual a regra de rotacao do
        // chassi no controller; em marcha a re o volante inverte.
        float target = 0f;
        if (Mathf.Abs(ctrl.CurrentSpeed) > 0.15f)
            target = ctrl.SteerInput * maxSteerAngle * Mathf.Sign(ctrl.CurrentSpeed);

        steerAngle = Mathf.Lerp(steerAngle, target, 1f - Mathf.Exp(-steerLerp * dt));
        Quaternion q = Quaternion.Euler(0f, steerAngle, 0f);
        if (steerLeft != null) steerLeft.localRotation = q;
        if (steerRight != null) steerRight.localRotation = q;
    }
}
