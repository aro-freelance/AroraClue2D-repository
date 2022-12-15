using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Nakama;
using System.Threading.Tasks;

public class ClientObject : MonoBehaviour
{
    const string scheme = "http";
    const string host = "127.0.0.1";
    const int port = 7350;
    const string serverKey = "defaultkey";

    const string prefKeyName = "nakama.session";

    /*
     * Requests
     * The client includes lots of builtin APIs for various features of the game server. 
     * These can be accessed with the async methods. It can also call custom logic as RPC functions on the server.
     * These can also be executed with a socket object.
     * All requests are sent with a session object which authorizes the client.
     */
    ISession session;

    IApiAccount account;

    ISocket socket;


    void Start()
    {

        Debug.Log("Client Object Script. Ping.");

        var client = new Client(scheme, host, port, serverKey);

        //It is recommended to store the auth token from the session and check at startup if it has expired.
        //If the token has expired you must reauthenticate. The expiry time of the token can be changed as a setting in the server.
        var authToken = PlayerPrefs.GetString(prefKeyName);
        if (string.IsNullOrEmpty(authToken) || (session = Session.Restore(authToken)).IsExpired)
        {
            Debug.Log("Session has expired. Must reauthenticate!");
            AuthenticateAsync(client);
        };
        Debug.Log(session);


        if (session != null)
        {
            GetAccount(session, client);
            CreateSocket(session, client);
        }

    }

    /*
     * There's a variety of ways to authenticate with the server.
     * Authentication can create a user if they don't already exist with those credentials.
     * It's also easy to authenticate with a social profile from Google Play Games, Facebook, Game Center, etc.
     */
    async void AuthenticateAsync(Client client)
    {

        var deviceId = SystemInfo.deviceUniqueIdentifier;
        try
        {
            session = await client.AuthenticateDeviceAsync(deviceId);
        }
        catch (ApiResponseException e)
        {
            Debug.Log(e);
        }


        //When authenticated the server responds with an auth token (JWT) which contains useful properties and gets deserialized into a Session object.
        Debug.Log(session);
        Debug.Log(session.AuthToken); // raw JWT token
        Debug.LogFormat("Session user id: '{0}'", session.UserId);
        Debug.LogFormat("Session user username: '{0}'", session.Username);
        Debug.LogFormat("Session has expired: {0}", session.IsExpired);
        Debug.LogFormat("Session expires at: {0}", session.ExpireTime); // in seconds.


        if (session != null)
        {
            GetAccount(session, client);
            CreateSocket(session, client);
        }

    }

    async void GetAccount(ISession session, Client client)
    {

        //Three ways to get account... A. No retry, B. Local retry config, C. Global retry config

        //A. GET ACCOUNT (No retry)
        // account = await client.GetAccountAsync(session);




        //Requests can be supplied with a retry configurations in cases of transient network or server errors.
        //B. PER-REQUEST RETRY

        var retryConfiguration = new RetryConfiguration(baseDelayMs: 1, maxRetries: 5, delegate { System.Console.WriteLine("about to retry."); });
        try
        {
            account = await client.GetAccountAsync(session, retryConfiguration);
        }
        catch (ApiResponseException e)
        {
            Debug.Log(e);
        }


        Debug.LogFormat("User id: '{0}'", account.User.Id);
        Debug.LogFormat("User username: '{0}'", account.User.Username);
        Debug.LogFormat("Account virtual wallet: '{0}'", account.Wallet);


        //C. GLOBAL RETRY
        //     var retryConfiguration = new RetryConfiguration(baseDelayMs: 1, maxRetries: 5, delegate { System.Console.WriteLine("about to retry."); });
        //     client.GlobalRetryConfiguration = retryConfiguration;
        //     account = await client.GetAccountAsync(session);



        //Cancel the request mid-flight like this:
        //   var canceller = new System.Threading.CancellationToken();
        //   account = await client.GetAccountAsync(session, retryConfiguration: null, canceller);

        //   await Task.Delay(25);

        //   canceller.Cancel(); // will raise a TaskCanceledException


    }

    /*
     * The client can create one or more sockets with the server.
     * Each socket can have it's own event listeners registered for responses received from the server.
    */
    async void CreateSocket(ISession session, Client client)
    {

        socket = client.NewSocket(useMainThread: false);
        socket.Connected += () => Debug.Log("Socket connected.");
        socket.Closed += () => Debug.Log("Socket closed.");

        try
        {
            await socket.ConnectAsync(session);
        }
        catch (ApiResponseException e)
        {
            Debug.Log(e);
        }




        //FOR WebGL Builds
        /*
         * var client = new Client("defaultkey", UnityWebRequestAdapter.Instance);
         * var socket = client.NewSocket();
         * // or
         * #if UNITY_WEBGL && !UNITY_EDITOR
         * ISocketAdapter adapter = new JsWebSocketAdapter();
         * #else
         * ISocketAdapter adapter = new WebSocketAdapter();
         * #endif
         * var socket = Socket.From(client, adapter);
         * 
         */

    }

}
