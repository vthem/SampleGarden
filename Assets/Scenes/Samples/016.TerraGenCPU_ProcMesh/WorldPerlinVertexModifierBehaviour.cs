#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;

namespace _016_TerraGenCPU_ProcMesh
{
	[ExecuteInEditMode]
	public class WorldPerlinVertexModifierBehaviour : VertexModifierBehaviourBase, IVertexModifierGetter
	{
		public Vector3 perlinOffset = new Vector3(0.232f, 0.329879f, 0.2398732f);

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
			v += perlinOffset;
			return new Vector3(xVal, Mathf.PerlinNoise(v.x, v.z), zVal);
		}

		//public float PerlinScale { get => perlinScale; set => perlinScale = value; }
		//public Vector3 LocalPosition { get => localPosition; set => localPosition = value; }

		#region private
		//[SerializeField] protected float perlinScale;
		//[SerializeField] protected Vector3 localPosition;

		//protected Matrix4x4 perlinMatrix;

		private void Update()
		{
			//LocalPosition = transform.position;
		}

		#endregion // private
	}



#if UNITY_EDITOR
	[CustomEditor(typeof(WorldPerlinVertexModifierBehaviour))]
	public class WorldPerlinVertexModifierEditor: Editor
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