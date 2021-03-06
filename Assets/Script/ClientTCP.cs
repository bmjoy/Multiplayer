﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Sockets;
using System.Net;
using System;

public class ClientTCPConnection {

    private ClientPacketHandler packetHandler;
    private static Socket _clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    private bool receive = false;
    
    //private byte[] _asyncBuffer = new byte[1024];

    public ClientTCPConnection(ClientPacketHandler packetHandler) {
        this.packetHandler = packetHandler;
    }
    
    public void ConnectToServer() {
        Logger.Log("Connecting to server..");
        _clientSocket.BeginConnect("127.0.0.1", 5555, new AsyncCallback(ConnectCallback), _clientSocket);
    }

    private void ConnectCallback(IAsyncResult result) {
        _clientSocket.EndConnect(result);
        receive = true;
        Logger.Log("Connected to server. Receiving data..");

        while(receive) {
            OnReceive();
        }
    }

    private void OnReceive() {
        byte[] _sizeInfo = new byte[4];
        //byte[] _receivedBuffer = new byte[1024];

        int totalRead = 0;
        int currentRead = 0;

        try {
            currentRead = totalRead = _clientSocket.Receive(_sizeInfo);
            if(totalRead <= 0) {
                Logger.Log("You are not connected to the server (readbuffer 0 bytes)");
            }
            else {
                while(totalRead < _sizeInfo.Length && currentRead > 0) {
                    currentRead = _clientSocket.Receive(_sizeInfo, totalRead, _sizeInfo.Length - totalRead, SocketFlags.None);
                    totalRead += currentRead;
                }

                int messagesize = 0;
                messagesize |= _sizeInfo[0];
                messagesize |= (_sizeInfo[1] << 8);
                messagesize |= (_sizeInfo[2] << 16);
                messagesize |= (_sizeInfo[3] << 24);

                byte[] data = new byte[messagesize];

                totalRead = 0;
                currentRead = totalRead = _clientSocket.Receive(data, totalRead, data.Length - totalRead, SocketFlags.None);

                while(totalRead < messagesize && currentRead > 0) {
                    currentRead = _clientSocket.Receive(data, totalRead, data.Length - totalRead, SocketFlags.None);
                    totalRead += currentRead;
                }

                // Handle network information
                packetHandler.HandlePacket(data);
            }
        }
        catch(Exception ex) {
            Logger.Log("You are not connected to the server: " + ex.Message);
        }
    }

    public void SendData(byte[] data) {
        _clientSocket.Send(data);
    }

    public void ThankYouServer() {
        PacketBuffer buffer = new PacketBuffer();
        buffer.WriteInteger((int)ClientPackets.CThankYou);
        buffer.WriteString("Connection acknowledge by client");
        SendData(buffer.ToArray());
        buffer.Dispose();
    }

    public void Disconnect() {
        Debug.Log("Disconnecting client");
        receive = false;
        _clientSocket.Close();
    }

}
