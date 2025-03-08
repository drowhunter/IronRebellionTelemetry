using System.Net;
using System.Net.Sockets;
using System.Threading;
using System;
using BepInEx.Logging;

namespace IronRebellionTelemetry
{
    public class TelemetrySender
    {
        private static Thread senderThread;
        private static bool isRunning = false;
        private static UdpClient udpClient;
        private static int port = 6969; 
        private static string targetIP = "127.0.0.1";
        private static int interval = 20;

        private static int stompedIterations = 6; //How many iterations the the stomped signal should be set to true
        private static int stompedCounter = 0;
        private static int landedIterations = 6;
        private static int landedCounter = 0;
        private static int jumpedIterations = 6;
        private static int jumpedCounter = 0;
        private static int weaponFiredIterations = 6;
        private static int weaponFiredCounter = 0;

        public static void Start()
        {
            if (isRunning) return;

            isRunning = true;
            udpClient = new UdpClient();
            senderThread = new Thread(SendTelemetry);
            senderThread.IsBackground = true;
            senderThread.Start();

            BepInExPlugin.Log.LogInfo("TelemetrySender started");
        }

        public static void Stop()
        {
            isRunning = false;
            udpClient?.Close();
            BepInExPlugin.Log.LogInfo("TelemetrySender stopped");
        }

        private static void SendTelemetry()
        {
            while (isRunning)
            {
                try
                {
                    byte[] data = new byte[51]; 

                    // Copy float values (48 bytes in total)
                    Buffer.BlockCopy(BitConverter.GetBytes(BepInExPlugin.velocityX), 0, data, 0, 4);
                    Buffer.BlockCopy(BitConverter.GetBytes(BepInExPlugin.velocityY), 0, data, 4, 4);
                    Buffer.BlockCopy(BitConverter.GetBytes(BepInExPlugin.velocityZ), 0, data, 8, 4);

                    Buffer.BlockCopy(BitConverter.GetBytes(BepInExPlugin.angularX), 0, data, 12, 4);
                    Buffer.BlockCopy(BitConverter.GetBytes(BepInExPlugin.angularY), 0, data, 16, 4);
                    Buffer.BlockCopy(BitConverter.GetBytes(BepInExPlugin.angularZ), 0, data, 20, 4);

                    Buffer.BlockCopy(BitConverter.GetBytes(BepInExPlugin.rotationX), 0, data, 24, 4);
                    Buffer.BlockCopy(BitConverter.GetBytes(BepInExPlugin.rotationY), 0, data, 28, 4);
                    Buffer.BlockCopy(BitConverter.GetBytes(BepInExPlugin.rotationZ), 0, data, 32, 4);

                    Buffer.BlockCopy(BitConverter.GetBytes(BepInExPlugin.currentTilt), 0, data, 36, 4);
                    Buffer.BlockCopy(BitConverter.GetBytes(BepInExPlugin.currentLean), 0, data, 40, 4);

                    data[44] = (byte)(BepInExPlugin.isFlying ? 1 : 0);
                    data[45] = (byte)(BepInExPlugin.isRunning ? 1 : 0);
                    data[46] = (byte)(BepInExPlugin.isHit ? 1 : 0);
                    data[47] = (byte)(BepInExPlugin.weaponFired ? 1 : 0);
                    data[48] = (byte)(BepInExPlugin.stomped ? 1 : 0);
                    data[49] = (byte)(BepInExPlugin.landed ? 1 : 0);
                    data[50] = (byte)(BepInExPlugin.jumped ? 1 : 0);



                    udpClient.Send(data, data.Length, targetIP, port);
                    if (BepInExPlugin.stomped)
                    {
                        stompedCounter++;
                    }
                    if (BepInExPlugin.landed)
                    {
                        landedCounter++;
                    }
                    if (BepInExPlugin.jumped)
                    {
                        jumpedCounter++;
                    }
                    if (BepInExPlugin.weaponFired)
                    {
                        weaponFiredCounter++;
                    }

                    if (BepInExPlugin.stomped && stompedCounter > stompedIterations)
                    {
                        BepInExPlugin.stompedSend = true;
                        stompedCounter = 0;
                    }
                    if (BepInExPlugin.landed && landedCounter > landedIterations)
                    {
                        BepInExPlugin.landedSend = true;
                        landedCounter = 0;
                    }
                    if (BepInExPlugin.jumped && jumpedCounter > jumpedIterations)
                    {
                        BepInExPlugin.jumpedSend = true;
                        jumpedCounter = 0;
                    }
                    if (BepInExPlugin.weaponFired && weaponFiredCounter > weaponFiredIterations)
                    {
                        BepInExPlugin.weaponFiredSend = true;
                        weaponFiredCounter = 0;
                    }
                }
                catch (Exception ex)
                {
                    BepInExPlugin.Log.LogError($"TelemetrySender Error: {ex.Message}");
                }

                Thread.Sleep(interval);
            }
        }
    }
}
