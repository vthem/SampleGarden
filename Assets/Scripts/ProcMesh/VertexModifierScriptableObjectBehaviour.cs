using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VertexModifierScriptableObjectBehaviour : MonoBehaviour, IVertexModifierGetter
{
	public VertexModifierScriptableObject vertexModifier;

	public IVertexModifier VertexModifier => vertexModifier;
}
