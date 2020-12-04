using System;
using Loxodon.Framework.Contexts;
using Loxodon.Framework.Examples;
using Loxodon.Framework.Services;
using Loxodon.Framework.Views;
using Loxodon.Log;
using UnityEngine;
using UnityEngine.UI;
using Zitga.Sound;

public class SoundManagerDemo : MonoBehaviour
{
    public EazySoundDemoAudioControls[] AudioControls;
    public Slider globalVolSlider;
    public Slider globalMusicVolSlider;
    public Slider globalSoundVolSlider;

    private SoundManager soundManager;

    private ApplicationContext context;

    private GlobalWindowManager windowManager;

    private GlobalUpdateSystem globalUpdateSystem;

    private void Awake()
    {
        context = Context.GetApplicationContext();

        RegisterService();
    }

    private void RegisterService()
    {
        IServiceContainer container = context.GetContainer();

        /* Initialize the ui view locator and register UIViewLocator */
        container.Register<IUIViewLocator>(new ResourcesViewLocator());
            
        /* register IUpdateSystem */
        container.Register(new GlobalUpdateSystem());
           
        /* register IUpdateSystem */
        container.Register(new SoundManager());

        globalUpdateSystem = context.GetService <GlobalUpdateSystem>();

        soundManager = context.GetService<SoundManager>();

#if UNITY_EDITOR
        windowManager = FindObjectOfType<GlobalWindowManager>();
        if (windowManager != null)
            throw new NotFoundException("Exist the GlobalWindowManager.");
#endif
        windowManager = GetComponentInChildren<Canvas>().gameObject.AddComponent<GlobalWindowManager>();
        /* register GlobalWindowManager */
        container.Register(windowManager);
    }

    private void Update ()
    {
        globalUpdateSystem.OnUpdate(Time.deltaTime);
        
        // Update UI
        for(int i=0; i < AudioControls.Length; i++)
        {
            EazySoundDemoAudioControls audioControl = AudioControls[i];
            if (audioControl.audio != null && audioControl.audio.IsPlaying)
            {
                if (audioControl.pauseButton != null)
                {
                    audioControl.playButton.interactable = false;
                    audioControl.pauseButton.interactable = true;
                    audioControl.stopButton.interactable = true;
                    audioControl.pausedStatusTxt.enabled = false;
                }
            }
            else if (audioControl.audio != null && audioControl.audio.Paused)
            {
                if (audioControl.pauseButton != null)
                {
                    audioControl.playButton.interactable = true;
                    audioControl.pauseButton.interactable = false;
                    audioControl.stopButton.interactable = false;
                    audioControl.pausedStatusTxt.enabled = true;
                }
            }
            else
            {
                if (audioControl.pauseButton != null)
                {
                    audioControl.playButton.interactable = true;
                    audioControl.pauseButton.interactable = false;
                    audioControl.stopButton.interactable = false;
                    audioControl.pausedStatusTxt.enabled = false;
                }
            }
        }
	}

    public void PlayMusic1()
    {
        EazySoundDemoAudioControls audioControl = AudioControls[0];

        if (audioControl.audio == null)
        {
            int audioID = soundManager.PlayMusic(audioControl.audioclip, audioControl.volumeSlider.value, true, false);
            AudioControls[0].audio = soundManager.GetAudio(audioID);
        }
        else if (audioControl.audio != null && audioControl.audio.Paused)
        {
            audioControl.audio.Resume();
        }
        else
        {
            audioControl.audio.Play();
        }
    }

    public void PlayMusic2()
    {
        EazySoundDemoAudioControls audioControl = AudioControls[1];

        if (audioControl.audio == null)
        {
            int audioID = soundManager.PlayMusic(audioControl.audioclip, audioControl.volumeSlider.value, true, false);
            AudioControls[1].audio = soundManager.GetAudio(audioID);
        }
        else if (audioControl.audio != null && audioControl.audio.Paused)
        {
            audioControl.audio.Resume();
        }
        else
        {
            audioControl.audio.Play();
        }
    }

    public void PlaySound1()
    {
        EazySoundDemoAudioControls audioControl = AudioControls[2];
        int audioID = soundManager.PlaySound(audioControl.audioclip, audioControl.volumeSlider.value);

        AudioControls[2].audio = soundManager.GetAudio(audioID);
    }

    public void PlaySound2()
    {
        EazySoundDemoAudioControls audioControl = AudioControls[3];
        int audioID = soundManager.PlaySound(audioControl.audioclip, audioControl.volumeSlider.value);

        AudioControls[3].audio = soundManager.GetAudio(audioID);
    }

    public void Pause(string audioControlIDStr)
    {
        int audioControlID = int.Parse(audioControlIDStr);
        EazySoundDemoAudioControls audioControl = AudioControls[audioControlID];

        audioControl.audio.Pause();
    }

    public void Stop(string audioControlIDStr)
    {
        int audioControlID = int.Parse(audioControlIDStr);
        EazySoundDemoAudioControls audioControl = AudioControls[audioControlID];

        audioControl.audio.Stop();
    }

    public void AudioVolumeChanged(string audioControlIDStr)
    {
        int audioControlID = int.Parse(audioControlIDStr);
        EazySoundDemoAudioControls audioControl = AudioControls[audioControlID];

        if (audioControl.audio != null)
        {
            audioControl.audio.SetVolume(audioControl.volumeSlider.value, 0);
        }
    }

    public void GlobalVolumeChanged()
    {
        soundManager.GlobalVolume = globalVolSlider.value;
    }

    public void GlobalMusicVolumeChanged()
    {
        soundManager.GlobalMusicVolume = globalMusicVolSlider.value;
    }

    public void GlobalSoundVolumeChanged()
    {
        soundManager.GlobalSoundsVolume = globalSoundVolSlider.value;
    }
}

[System.Serializable]
public struct EazySoundDemoAudioControls
{
    public AudioClip audioclip;
    public Audio audio;
    public Button playButton;
    public Button pauseButton;
    public Button stopButton;
    public Slider volumeSlider;
    public Text pausedStatusTxt;
}
