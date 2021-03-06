using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace _007_BezierPath
{
	[ExecuteInEditMode]
	public class BezierPathBehaviour : MonoBehaviour
	{
		[SerializeField]
		private readonly BezierPath bezierPath = new BezierPath();

		public BezierPath Path => bezierPath;
	}

#if UNITY_EDITOR
	[CustomEditor(typeof(BezierPathBehaviour)), CanEditMultipleObjects]
	public class BezierPathEditor : Editor
	{
		private bool debugDrawHandle = true;

		public override void OnInspectorGUI()
		{
			BezierPathBehaviour pathBehaviour = (BezierPathBehaviour)target;
			BezierPath path = pathBehaviour.Path;

			if (GUILayout.Button("add begin"))
			{
				path.AddBegin();
				SceneView.RepaintAll();
			}
			if (GUILayout.Button("add end"))
			{
				path.AddEnd();
				SceneView.RepaintAll();
			}
			if (GUILayout.Button("clear"))
			{
				path.Clear();
				SceneView.RepaintAll();
			}
			debugDrawHandle = EditorGUILayout.Toggle("debug draw handle", debugDrawHandle);
			DrawDefaultInspector();
		}

		protected virtual void OnSceneGUI()
		{
			BezierPathBehaviour pathBehaviour = (BezierPathBehaviour)target;
			BezierPath path = pathBehaviour.Path;
			Transform transform = (target as MonoBehaviour).transform;
			for (int i = 0; i < path.Count; ++i)
			{
				BezierSegment segment = path.GetSegment(i);
				if (debugDrawHandle)
				{
					if (BezierSegmentBehaviour.DrawBezierHandles(ref segment, $"{i}", target))
					{
						path.SetSegment(i, segment);
					}
				}
				else
				{
					BezierSegmentBehaviour.DrawSegment(segment);
				}
			}
		}
	}
#endif
}