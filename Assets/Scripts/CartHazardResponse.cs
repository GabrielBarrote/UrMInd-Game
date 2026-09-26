using System.Collections.Generic;
using UnityEngine;

// Reacao do rover a buracos e bueiros.
//
// Esses defeitos tem collider TRIGGER (nao bloqueiam a passagem, mas sao
// detectaveis). O CharacterController dispara OnTriggerEnter ao entrar neles.
//
// A reacao propriamente dita e aplicada por AnalysisCartController.ApplyImpact:
// 'speed' e 'velY' sao estado privado do movimento, e mexer neles daqui criaria
// um segundo sistema de movimentacao.
[RequireComponent(typeof(AnalysisCartController))]
public class CartHazardResponse : MonoBehaviour
{
    [Header("Buraco no pavimento")]
    public float holeSpeedKeep = 0.55f;    // fracao da velocidade que sobra
    public float holeKick = 2.4f;
    public float holeShake = 0.55f;

    [Header("Bueiro (mais severo)")]
    public float manholeSpeedKeep = 0.35f;
    public float manholeKick = 3.2f;
    public float manholeShake = 0.85f;

    [Header("Condicoes")]
    public float minSpeed = 1.5f;          // parado nao toma tranco
    public float reArmSeconds = 1.2f;      // evita repetir no mesmo defeito

    AnalysisCartController ctrl;
    readonly Dictionary<EntityId, float> lastHit = new Dictionary<EntityId, float>();

    void Awake()
    {
        ctrl = GetComponent<AnalysisCartController>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (Mathf.Abs(ctrl.CurrentSpeed) < minSpeed) return;

        // o trigger esta na mesh filha; o DefectInfo esta no objeto do defeito
        DefectInfo info = other.GetComponentInParent<DefectInfo>();
        if (info == null || info.defectType == null) return;

        bool isHole = info.defectType.IndexOf("Buraco") >= 0;
        bool isManhole = info.defectType.IndexOf("Bueiro") >= 0;
        if (!isHole && !isManhole) return;

        // Um buraco tem varias meshes, logo varios triggers sobrepostos.
        // Sem esta guarda o tranco dispararia varias vezes no mesmo defeito.
        var id = info.gameObject.GetEntityId();
        float now = Time.time;
        float last;
        if (lastHit.TryGetValue(id, out last) && now - last < reArmSeconds) return;
        lastHit[id] = now;

        if (isManhole)
            ctrl.ApplyImpact(manholeSpeedKeep, manholeKick, manholeShake);
        else
            ctrl.ApplyImpact(holeSpeedKeep, holeKick, holeShake);
    }
}
