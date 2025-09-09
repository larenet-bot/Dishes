using UnityEngine;

[System.Serializable]
public class Employee
{
    public string name;
    public float cost;
    public float profitPerSecond;
    public int count;

    public Employee(string name, float cost, float profitPerSecond)
    {
        this.name = name;
        this.cost = cost;
        this.profitPerSecond = profitPerSecond;
        this.count = 0;
    }

    public float GetTotalProfitPerSecond() => profitPerSecond * count;
}