using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CarDefinition", menuName = "Racing/Car Definition")]
public class CarDefinition : ScriptableObject
{
    [Header("Identity")]
    public string carName = "New Car";
    public string creator = "Unknown";

    [Header("Preview")]
    public GameObject previewPrefab;

    [Header("Drive Stats")]
    public CarDriveStats driveStats = new CarDriveStats();

    [Header("UI Stats")]
    public List<CarStatLine> customStats = new List<CarStatLine>();
}

[Serializable]
public class CarDriveStats
{
    public float maxForwardSpeed = 8f;
    public float maxReverseSpeed = 4f;
    public float acceleration = 12f;
    public float friction = 4f;
    public float turnSpeed = 120f;
    public float turnMagnitude = 1.2f;
    [Range(0f, 1f)] public float highSpeedTurnMultiplier = 0.25f;
    public float minimumSteerSpeed = 0.05f;
}

[Serializable]
public class CarStatLine
{
    public string label = "Stat";
    public string value = "0";
}
