using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

public class Bootstrap : MonoBehaviour
{

    public string subscriptionKey;
    public GameObject speechButton;
    private bool speeching = false;
    private string device;
    private AudioClip recordClip;
    private string token;
    private Timer accessTokenRenewer;
    private const int RefreshTokenDuration = 9;

    // Use this for initialization
    void Start()
    {
        Authentication();
    }

    private void Authentication()
    {
        FetchToken();

        accessTokenRenewer = new Timer(new TimerCallback(OnTokenExpiredCallback),
                                           this,
                                           TimeSpan.FromMinutes(RefreshTokenDuration),
                                           TimeSpan.FromMilliseconds(-1));
    }

    private void FetchToken()
    {
        var req = HttpWebRequest.Create("https://api.cognitive.microsoft.com/sts/v1.0/issueToken");
        req.Headers.Add("Ocp-Apim-Subscription-Key", subscriptionKey);
        req.Method = "POST";
        req.BeginGetResponse(new AsyncCallback(FetchTokenHandler), req);
    }

    private void OnTokenExpiredCallback(object state)
    {
        FetchToken();

        accessTokenRenewer.Change(TimeSpan.FromMinutes(RefreshTokenDuration), TimeSpan.FromMilliseconds(-1));
    }

    private void FetchTokenHandler(IAsyncResult result)
    {
        var req = (HttpWebRequest)result.AsyncState;
        var res = req.EndGetResponse(result);
        var sr = new StreamReader(res.GetResponseStream());

        token = sr.ReadToEnd();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void SwitchSpeech()
    {
        if (speeching)
        {
            StopSpeech();
        }
        else
        {
            StartSpeech();
        }

        speeching = !speeching;
    }

    private void StartSpeech()
    {
        device = Microphone.devices[0];
        recordClip = Microphone.Start(device, false, 10, 44100);
        speechButton.GetComponentInChildren<Text>().text = "Stop";
    }

    private void StopSpeech()
    {
        Microphone.End(device);
        device = null;
        var data = WavUtility.FromAudioClip(recordClip);
        recordClip = null;
        CallAPI(data);
        speechButton.GetComponentInChildren<Text>().text = "Start";
    }

    private void CallAPI(byte[] data)
    {
        var req = (HttpWebRequest)HttpWebRequest.Create("https://speech.platform.bing.com/speech/recognition/interactive/cognitiveservices/v1?locale=zh-TW&format=detailed&requestid=39530efe-5677-416a-98b0-93e13ec93c2b");
        req.SendChunked = true;
        req.Accept = @"application/json;text/xml";
        req.Method = "POST";
        req.ProtocolVersion = HttpVersion.Version11;
        req.ContentType = @"audio/wav; codec=""audio/pcm""; samplerate=16000";
        req.Headers["Authorization"] = "Bearer " + token;

        using (Stream stream = req.GetRequestStream())
        {
            stream.Write(data, 0, data.Length);
            stream.Flush();
        }

        Console.WriteLine("Response:");
        var responseString = "";
        using (WebResponse response = req.GetResponse())
        {
            Console.WriteLine(((HttpWebResponse)response).StatusCode);

            using (StreamReader sr = new StreamReader(response.GetResponseStream()))
            {
                responseString = sr.ReadToEnd();
            }

            Console.WriteLine(responseString);
            Console.ReadLine();
        }
    }

    private void CallAPIHanlder(IAsyncResult result)
    {
        var req = (HttpWebRequest)result.AsyncState;
        var res = req.EndGetResponse(result);
        var sr = new StreamReader(res.GetResponseStream());

        Debug.Log(sr.ReadToEnd());
    }
}
