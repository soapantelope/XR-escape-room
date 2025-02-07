using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;


public class HandDance : NetworkBehaviour
{
    [SerializeField] private float cooldown;
    [SerializeField] private ulong poserId;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private ParticleSystem particleSystem;
    public TMPro.TextMeshProUGUI handPoseText;

    private bool coolingDown = false;
    private float cooldownTimer;

    public NetworkVariable<int> currentPose = new NetworkVariable<int>(1);
    [SerializeField] private ParticleSystem[] progressParticles;

    public void OnPoseCorrect(int poseIndex)
    {
        Debug.Log("Pose Correct: " + poseIndex);
        if (IsClient)
        {
            RequestPoseCorrectServerRpc(poseIndex);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestPoseCorrectServerRpc(int poseIndex, ServerRpcParams serverRpcParams = default)
    {
        Debug.Log("Requested from client " + serverRpcParams.Receive.SenderClientId);
        if (serverRpcParams.Receive.SenderClientId == poserId)
        {
            if (poseIndex == currentPose.Value)
            {
                currentPose.Value += 1;

                // Change to client RPC later
                if (currentPose.Value > 3)
                {
                    OnHandDanceCompletionClientRpc();

                }
                cooldownTimer = cooldown;
                coolingDown = true;
            }
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            currentPose.OnValueChanged += PlayParticlesClientRpc;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            currentPose.OnValueChanged -= PlayParticlesClientRpc;
        }
    }

    [ClientRpc]
    private void PlayParticlesClientRpc(int oldPoseNumber, int newPoseNumber) {
        if (newPoseNumber == 1) {
            for (int i = 0; i < progressParticles.Length; i++) {
                progressParticles[i].Stop();
            }
        }
        else {
            progressParticles[newPoseNumber - 2].Play();
        }
        if (NetworkManager.Singleton.LocalClientId == poserId) { 
            particleSystem.Play();
        }
    }

    [ClientRpc]
    private void OnHandDanceCompletionClientRpc()
    {
        Debug.Log("Hand Dance Completed");
        handPoseText.text = "Hand Dance Complete";
        gameManager.OnRoomSolved();
        this.gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (coolingDown) {
            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer <= 0)
            {
                currentPose.Value = 1;
                Debug.Log("Hand Dance Reset");
                coolingDown = false;
            }
        }
        
    }
}
