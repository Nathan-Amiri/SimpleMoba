using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SceneReference : MonoBehaviour
{
    public List<Image> hudAbilities = new();

    public List<TMP_Text> hudAbilityCharges = new();

    public Transform petParent;
}