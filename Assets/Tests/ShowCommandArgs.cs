using Rhinox.Utilities;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ShowCommandArgs : MonoBehaviour
{
    [SerializeField] private TextMeshPro _textMeshPro;

    // Start is called before the first frame update
    private IEnumerator Start()
    {
        while (!ConfigFileManager.AllConfigsLoaded)
        {
            yield return null;
        }

        Debug.Log($"Bool:\t{TestConfig.Instance.boolean}");
        Debug.Log($"float:\t{TestConfig.Instance.floaty}");
        Debug.Log($"int:\t{TestConfig.Instance.wholeNumb}");
        Debug.Log($"name:\t{TestConfig.Instance.name}");

        _textMeshPro.text += TestConfig.Instance.boolean.ToString();
        _textMeshPro.text += "\n";
        _textMeshPro.text += TestConfig.Instance.floaty.ToString();
        _textMeshPro.text += "\n";
        _textMeshPro.text += TestConfig.Instance.wholeNumb.ToString();
        _textMeshPro.text += "\n";
        _textMeshPro.text += TestConfig.Instance.name.ToString();


    }
}
