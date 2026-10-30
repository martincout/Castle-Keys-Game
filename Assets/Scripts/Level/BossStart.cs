using Cinemachine;
using SimplePlatformer.Enemy;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class BossStart : MonoBehaviour
{
    public new CinemachineVirtualCamera camera;
    public CinemachineVirtualCamera bossCamera;
    public BossBehaviour boss;
    public GameObject celldoor;
    public AudioMixer audioMixer;
    private bool triggered = false;
    [SerializeField] private HealthBar hb;

    public void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && !triggered)
        {
            triggered = true;
            camera.GetCinemachineComponent<CinemachineFramingTransposer>().m_ScreenY = 0.77f;
            StartCoroutine(FocusBoss(1f));
            boss.ChangeState(BossBehaviour.State.START);
            boss.healthSystem.SetHealthBar(hb);
            boss.DisplayHealthBar();
            celldoor.SetActive(true);
            Debug.Log(celldoor);
            audioMixer.FindSnapshot("Boss").TransitionTo(2f);
        }
    }

    private void OnEnable()
    {
        BossBehaviour.OnBossRespawn += Restart;
    }

    private void OnDisable()
    {
        BossBehaviour.OnBossRespawn -= Restart;

    }

    private void Restart(GameObject _boss)
    {
        boss = _boss.GetComponent<BossBehaviour>();
        celldoor.SetActive(false);
        triggered = false;
    }
    

    private IEnumerator FocusBoss(float _sec)
    {
        bossCamera.gameObject.SetActive(true);
        camera.gameObject.SetActive(false);
        yield return new WaitForSeconds(_sec);
        camera.gameObject.SetActive(true);
        bossCamera.gameObject.SetActive(false);
    }
}
