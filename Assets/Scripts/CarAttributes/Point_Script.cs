using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Point_Script : MonoBehaviour
{
    private float startingPoint;
    private float currentPoint;
    [SerializeField] private TextMeshProUGUI pointText;

    public CarController_Script CarControllerScript;

    public Animator animUTurn;
    private void Start()
    {
        startingPoint = gameObject.transform.position.z;
        currentPoint = startingPoint;
    }

    private void Update()
    {
        if (CarControllerScript.currentHealth == 0)
        {
            pointText.text = "Punkte: " + currentPoint.ToString("00");
        }
        else
        {
            currentPoint = gameObject.transform.position.z;

            float distance = Mathf.Abs(startingPoint - currentPoint);
            if (distance > 15) // reset scene when car move backward too far
                CarControllerScript.ResetScene();
            else if (currentPoint < startingPoint)
            {
                pointText.text = "Punkte: 00";
                animUTurn.Play("UTurn");
            }
            else
            {
                pointText.text = "Punkte: " + distance.ToString("00");
            }
        }
    }
}
