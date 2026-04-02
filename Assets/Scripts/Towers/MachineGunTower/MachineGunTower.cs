using UnityEngine;

[DisallowMultipleComponent]
public class MachineGunTower : MonoBehaviour
{
    public Transform turret;
    public Transform firePoint;

    private Tower tower;

    private void Awake()
    {
        tower = GetComponent<Tower>();
        if (tower == null) return;

        if (turret == null)
        {
            turret = transform.Find("Turret");
            if (turret == null) turret = transform.Find("Head");
        }

        if (firePoint == null && turret != null)
            firePoint = turret.Find("FirePoint");

        if (firePoint == null)
            firePoint = transform.Find("FirePoint");

        tower.rotatingPart = turret;
        if (tower.firePoint == null) tower.firePoint = firePoint;
    }
}
