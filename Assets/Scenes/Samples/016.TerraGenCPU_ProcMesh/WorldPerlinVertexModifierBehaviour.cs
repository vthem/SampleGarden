#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;

namespace _016_TerraGenCPU_ProcMesh
{
	[ExecuteInEditMode]
	public class WorldPerlinVertexModifierBehaviour : VertexModifierBehaviourBase, IVertexModifierGetter
	{
		public Vector3 PerlinOffset { get => perlinOffset; set { RequireUpdate |= value.SetTo(ref perlinOffset); } }
		public float PerlinScale { get => perlinScale; set { RequireUpdate |= value.SetTo(ref perlinScale); } }

		public IVertexModifier VertexModifier => this;

		public override bool Initialize()
		{
			if (!base.Initialize())
			{
				return false;
			}

			//perlinMatrix = Matrix4x4.TRS(Vector3.one, Quaternion.identity, Vector3.one * perlinScale);

			return true;
		}

		public override Vector3 Vertex(int x, int z)
		{
			float xVal = xStart + x * xDelta;
			float zVal = zStart + z * zDelta;
			//var v = perlinMatrix.MultiplyPoint(new Vector3(xVal, 0, zVal));
			Vector3 v = transform.TransformPoint(new Vector3(xVal, 0, zVal));
			v += PerlinOffset;
			v *= PerlinScale;
			return new Vector3(xVal, Mathf.PerlinNoise(v.x, v.z), zVal);
		}

		#region private

		private Vector3 perlinOffset = new Vector3(0.232f, 0.329879f, 0.2398732f);
		private float perlinScale = 1f;

		#endregion // private
	}



#if UNITY_EDITOR
	[CustomEditor(typeof(WorldPerlinVertexModifierBehaviour))]
	public class WorldPerlinVertexModifierEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			DrawDefaultInspector();

			VertexModifierBehaviourBase vm = target as VertexModifierBehaviourBase;
			EditorGUILayout.LabelField($"RequireRebuild:{vm.RequireRebuild}");
			EditorGUILayout.LabelField($"RequireUpdate:{vm.RequireUpdate}");
		}
	}
#endif
}