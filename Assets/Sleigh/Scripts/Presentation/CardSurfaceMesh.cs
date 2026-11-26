using UnityEngine;
using UnityEngine.UI;

namespace Sleigh
{
    [DisallowMultipleComponent]
    public sealed class CardSurfaceMesh : BaseMeshEffect
    {
        CardSurfaceEffects owner;
        Material original;

        public void Bind(CardSurfaceEffects value)
        {
            if (graphic.material != value.SurfaceMaterial) original = graphic.material;
            owner = value;
            if (isActiveAndEnabled) graphic.material = owner.SurfaceMaterial;
            Refresh();
        }

        public void Refresh() { if (graphic) graphic.SetVerticesDirty(); }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (owner) graphic.material = owner.SurfaceMaterial;
        }

        protected override void OnDisable()
        {
            if (graphic && owner && graphic.material == owner.SurfaceMaterial) graphic.material = original;
            base.OnDisable();
        }

        public override void ModifyMesh(VertexHelper vertices)
        {
            if (!IsActive() || !owner) return;
            var rect = owner.Card.rect;
            var toCard = owner.Card.worldToLocalMatrix * transform.localToWorldMatrix;
            var vertex = new UIVertex();
            for (int i = 0; i < vertices.currentVertCount; i++)
            {
                vertices.PopulateUIVertex(ref vertex, i);
                Vector3 point = toCard.MultiplyPoint3x4(vertex.position);
                vertex.uv1 = new Vector4((point.x - rect.xMin) / Mathf.Max(1, rect.width),
                    (point.y - rect.yMin) / Mathf.Max(1, rect.height), owner.Dissolve, owner.SweepPhase);
                vertices.SetUIVertex(vertex, i);
            }
        }
    }
}
