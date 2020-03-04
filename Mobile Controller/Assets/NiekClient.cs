﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using TMPro;
using System.Text;
using System;
public class NiekClient : MonoBehaviour
{
    [SerializeField] private GameObject connectPanel;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private int port;

    private TcpClient activeClient;

    public string connectionIP;

    protected NetworkStream netStream = null;
    private byte[] netBuffer = new byte[49152];
    private int bytesReceived = 0;
    private string receivedMessage = "";

    public bool clientStarted;

    void Start()
    {
        clientStarted = false;
       
        StartCoroutine(test());
        Debug.Log("Script started!");
    }

    private IEnumerator test()
    {
        while(connectionIP == string.Empty)
        {
            yield return null;
        }
        StartClient();
    }


    public void StartClient()
    {
        statusText.text += "\nStarting Client";
        Debug.Log("Starting client!");
        activeClient = TryToConnect();
        StartCoroutine(ConnectionCheck());
    }



    private TcpClient TryToConnect()
    {
        TcpClient _client;
        try
        {
            statusText.text += "\nTrying to connect";
            IPAddress ipAd = IPAddress.Parse(connectionIP);
            _client = new TcpClient();
            _client.Connect(ipAd, port);
            if (_client.Connected)
            {
                clientStarted = true;
                activeClient = _client;
                netStream = _client.GetStream();
                StartCoroutine(ListenServerMessages());
                statusText.text += "\nConnected!";
                Debug.Log("Connected!");
                return _client;
            }
        }
        catch (SocketException e)
        {
            Debug.Log(e);
        }
        return null;
    }



    private IEnumerator ConnectionCheck()
    {
        while (activeClient.Connected)
        {
            yield return null;
            if (connectPanel.activeSelf) connectPanel.SetActive(false);
        }
        connectPanel.SetActive(true);
        statusText.text = "Connection lost!";
        CloseClient();
    }

    private void CloseClient()
    {
        clientStarted = false;
        if (activeClient.Connected)
            activeClient.Close();

        if (activeClient != null)
            activeClient = null;

        StopAllCoroutines();
    }
    private IEnumerator ListenServerMessages()
    {
        if (activeClient != null)
        {


        netStream = activeClient.GetStream();
        do
        {
            netStream.BeginRead(netBuffer, 0, netBuffer.Length, MessageReceived, null);

            if (bytesReceived > 0)
            {
                OnMessageReceived(receivedMessage);
                bytesReceived = 0;
            }
            
            yield return new WaitForSeconds(1);

        } while (bytesReceived >= 0 && netStream != null);
        CloseClient();
        }
    }

    private void MessageReceived(IAsyncResult result)
    {
        if (result.IsCompleted && activeClient.Connected)
        {
            bytesReceived = netStream.EndRead(result);
            receivedMessage = Encoding.ASCII.GetString(netBuffer, 0, bytesReceived);
        }
    }

    protected virtual void OnMessageReceived(string receivedMessage)
    {
        print(receivedMessage);
        if (this.receivedMessage == "Server_Close")
        {
            CloseClient();
        }
    }

    public void SendMessageToServer(string sendMsg)
    { 
        if (activeClient == null) return;
        if (!activeClient.Connected) return;
        byte[] msg = Encoding.ASCII.GetBytes(sendMsg);
        netStream.Write(msg, 0, msg.Length);
    }


}
