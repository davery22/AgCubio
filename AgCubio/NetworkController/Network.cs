﻿// Created by Daniel Avery and Keeton Hodgson
// November 2015

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace AgCubio
{
    /// <summary>
    /// Handles all networking code for AgCubio
    /// </summary>
    public static class Network
    {
        /// <summary>
        /// Callback delegate for networking
        /// </summary>
        public delegate void Callback(Preserved_State_Object state);

        /// <summary>
        /// Port number that this network code uses.
        /// </summary>
        private const int Port = 11000;

        /// <summary>
        /// Begins establishing a connection to the server
        /// </summary>
        public static Socket Connect_to_Server(Callback callback, string hostname)
        {
            // Store the server IP address and remote endpoint
            //   MSDN: localhost can be found with the "" string.
            IPAddress ipAddress = (hostname.ToUpper() == "LOCALHOST") ? IPAddress.Parse("::1") : IPAddress.Parse(hostname);
            IPEndPoint remoteEP = new IPEndPoint(ipAddress, Port);

            // Make a new socket and preserved state object and begin connecting
            Socket socket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            Preserved_State_Object state = new Preserved_State_Object(socket, callback);

            // Begin establishing a connection
            socket.BeginConnect(remoteEP, new AsyncCallback(Connected_to_Server), state);

            // Return the socket
            return state.socket;
        }


        /// <summary>
        /// Finish establishing a connection to the server, invoke the callback in the preserved state object, and begin receiving data
        /// </summary>
        public static void Connected_to_Server(IAsyncResult state_in_an_ar_object)
        {
            // Get the state from the parameter
            Preserved_State_Object state = (Preserved_State_Object)state_in_an_ar_object.AsyncState;

            try
            {
                state.socket.EndConnect(state_in_an_ar_object);

                // Invoke the callback
                state.callback.DynamicInvoke(state);

                // Begin receiving data from the server
                state.socket.BeginReceive(state.buffer, 0, Preserved_State_Object.BufferSize, 0, new AsyncCallback(ReceiveCallback), state);
            }
            catch (SocketException)
            {
                // If there is a problem with the socket, gracefully close it down
                if (state.socket.Connected)
                {
                    state.socket.Shutdown(SocketShutdown.Both);
                    state.socket.Close();
                }

                // Invoke the callback
                state.callback.DynamicInvoke(state);
            }
        }


        /// <summary>
        /// Handles reception and storage of data
        /// </summary>
        public static void ReceiveCallback(IAsyncResult state_in_an_ar_object)
        {
            // Get the state from the parameter, declare a variable for holding count of received bytes
            Preserved_State_Object state = (Preserved_State_Object)state_in_an_ar_object.AsyncState;

            try
            {
                int bytesRead = state.socket.EndReceive(state_in_an_ar_object);

                // If bytes were read, save the decoded string and invoke the callback
                if (bytesRead > 0)
                {
                    state.data.Append(Encoding.UTF8.GetString(state.buffer, 0, bytesRead));
                    state.callback.DynamicInvoke(state);
                }
                // Otherwise we are disconnected - close the socket
                else
                {
                    //TODO: do these have to stay commented out for it to work, or does it work now?
                    //Needs to be tested again.

                    state.socket.Shutdown(SocketShutdown.Both);
                    state.socket.Close();
                }
            }
            catch (Exception)
            {
                // If there is a problem with the socket, gracefully close it down
                if (state.socket.Connected)
                {
                    state.socket.Shutdown(SocketShutdown.Both);
                    state.socket.Close();
                }
            }
        }


        /// <summary>
        /// Tells server we are ready to receive more data
        /// </summary>
        public static void I_Want_More_Data(Preserved_State_Object state)
        {
            try
            {
                state.socket.BeginReceive(state.buffer, 0, Preserved_State_Object.BufferSize, 0, new AsyncCallback(ReceiveCallback), state);
            }
            catch (Exception)
            {
                // If there is a problem with the socket, gracefully close it down
                if (state.socket.Connected)
                {
                    state.socket.Shutdown(SocketShutdown.Both);
                    state.socket.Close();
                }
            }
        }


        /// <summary>
        /// Sends encoded data to the server
        /// </summary>
        public static void Send(Socket socket, String data, bool dbQuery = false)
        {
            // Set callback based on whether this is a DB send or a normal send
            AsyncCallback callback = dbQuery ? new AsyncCallback(QueryCallback) : new AsyncCallback(SendCallBack);

            byte[] byteData = Encoding.UTF8.GetBytes(data);
            Tuple<Socket, byte[]> state = new Tuple<Socket, byte[]>(socket, byteData);

            try
            {
                socket.BeginSend(byteData, 0, byteData.Length, 0, callback, state);
            }
            catch (Exception)
            {
                // If there is a problem with the socket, gracefully close it down
                if (socket.Connected)
                {
                    socket.Shutdown(SocketShutdown.Both);
                    socket.Close();
                }
            }
        }


        /// <summary>
        /// Helper method for Send - arranges for any leftover data to be sent
        /// </summary>
        public static void SendCallBack(IAsyncResult state_in_an_ar_object)
        {
            Tuple<Socket, byte[]> state = (Tuple<Socket, byte[]>)state_in_an_ar_object.AsyncState;

            try
            {
                int bytesSent = state.Item1.EndSend(state_in_an_ar_object);

                if (bytesSent == state.Item2.Length)
                    return;
                else
                {
                    byte[] bytes = new byte[state.Item2.Length - bytesSent];
                    Array.ConstrainedCopy(state.Item2, bytesSent, bytes, 0, bytes.Length);
                    Tuple<Socket, byte[]> newState = new Tuple<Socket, byte[]>(state.Item1, bytes);
                    state.Item1.BeginSend(state.Item2, bytesSent, state.Item2.Length, 0, new AsyncCallback(SendCallBack), newState);
                }
            }
            catch (Exception)
            {
                // If there is a problem with the socket, gracefully close it down
                if (state.Item1.Connected)
                {
                    state.Item1.Shutdown(SocketShutdown.Both);
                    state.Item1.Close();
                }
            }
        }


        /// <summary>
        /// Helper method for a DB Send - arranges for any leftover data to be sent, and the socket is then closed
        /// </summary>
        public static void QueryCallback(IAsyncResult state_in_an_ar_object)
        {
            Tuple<Socket, byte[]> state = (Tuple<Socket, byte[]>)state_in_an_ar_object.AsyncState;

            try
            {
                int bytesSent = state.Item1.EndSend(state_in_an_ar_object);

                if (bytesSent == state.Item2.Length) // If everything was sent, then close the socket
                {
                    state.Item1.Shutdown(SocketShutdown.Both);
                    state.Item1.Close();
                    return;
                }
                else
                {
                    byte[] bytes = new byte[state.Item2.Length - bytesSent];
                    Array.ConstrainedCopy(state.Item2, bytesSent, bytes, 0, bytes.Length);
                    Tuple<Socket, byte[]> newState = new Tuple<Socket, byte[]>(state.Item1, bytes);
                    state.Item1.BeginSend(state.Item2, bytesSent, state.Item2.Length, 0, new AsyncCallback(QueryCallback), newState);
                }
            }
            catch (Exception)
            {
                // If there is a problem with the socket, gracefully close it down
                if (state.Item1.Connected)
                {
                    state.Item1.Shutdown(SocketShutdown.Both);
                    state.Item1.Close();
                }
            }
        }


        /// <summary>
        /// Heart of the server code. Creates an async loop for accepting new clients.
        /// </summary>
        public static void Server_Awaiting_Client_Loop(Delegate callback, int port)
        {
            TcpListener server = TcpListener.Create(port);

            server.Start();
            Preserved_State_Object state = new Preserved_State_Object(server, callback);

            server.BeginAcceptSocket(new AsyncCallback(Accept_a_New_Client), state);
        }


        /// <summary>
        /// Accepts a new client and begins data transferring
        /// </summary>
        public static void Accept_a_New_Client(IAsyncResult ar)
        {
            Preserved_State_Object state = (Preserved_State_Object)ar.AsyncState;
            state.socket = state.server.EndAcceptSocket(ar);

            state.socket.BeginReceive(state.buffer, 0, Preserved_State_Object.BufferSize, 0, new AsyncCallback(ReceiveCallback), state); //Get the name, then give them their cube.
            state.server.BeginAcceptSocket(new AsyncCallback(Accept_a_New_Client), new Preserved_State_Object(state.server, state.callback));
        }
    }


    /// <summary>
    /// Preserves the state of the socket, and the current callback function
    /// </summary>
    public class Preserved_State_Object
    {
        /// <summary>
        /// Constructs a client-type Preserved_State_Object with the given socket and callback
        /// </summary>
        public Preserved_State_Object(Socket socket, Delegate callback)
        {
            this.socket = socket;
            this.callback = callback;
            data = new StringBuilder();
        }


        /// <summary>
        /// Constructs a server-type Preserved_State_Object with the given TcpListener and callback
        /// </summary>
        public Preserved_State_Object(TcpListener server, Delegate callback)
        {
            this.server = server;
            this.callback = callback;
            data = new StringBuilder();
        }

        /// <summary>
        /// Current callback function
        /// </summary>
        public Delegate callback;

        /// <summary>
        /// Buffer size of 2^10
        /// </summary>
        public const int BufferSize = 1024;

        /// <summary>
        /// Byte array for reading bytes from the server
        /// </summary>
        public byte[] buffer = new byte[BufferSize];

        /// <summary>
        /// Networking socket
        /// </summary>
        public Socket socket;

        /// <summary>
        /// String for storing cube data received from the server
        /// </summary>
        public StringBuilder data;

        /// <summary>
        /// ID for this player's cube. Used in the server
        /// </summary>
        public int CubeID;

        /// <summary>
        /// TcpListener the server uses
        /// </summary>
        public TcpListener server;
    }
}