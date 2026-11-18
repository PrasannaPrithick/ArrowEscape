using UnityEngine;

namespace HueGates.Game
{
    public sealed class HueAudio : MonoBehaviour
    {
        AudioSource effects;
        AudioSource music;
        AudioClip tick;
        AudioClip thud;
        AudioClip buzz;
        AudioClip win;
        SettingsData settings;

        public void Boot(SettingsData settings)
        {
            this.settings = settings;
            effects = gameObject.AddComponent<AudioSource>();
            effects.playOnAwake = false;
            music = gameObject.AddComponent<AudioSource>();
            music.playOnAwake = false;
            music.loop = true;
            music.volume = 0.12f;
            tick = Tone(880f, 0.09f, 18f);
            thud = Tone(150f, 0.16f, 10f);
            buzz = BuzzClip();
            win = Arpeggio();
            music.clip = Pad();
            Apply();
        }

        public void Apply()
        {
            if (settings != null && settings.music)
            {
                if (!music.isPlaying) music.Play();
            }
            else if (music.isPlaying)
            {
                music.Stop();
            }
        }

        public void Tick(int combo)
        {
            if (!On()) return;
            effects.pitch = 1f + (combo - 1) * 0.08f;
            effects.PlayOneShot(tick, 0.8f);
            effects.pitch = 1f;
        }

        public void Thud()
        {
            if (!On()) return;
            effects.pitch = 1f;
            effects.PlayOneShot(thud, 0.9f);
        }

        public void Buzz()
        {
            if (!On()) return;
            effects.pitch = 1f;
            effects.PlayOneShot(buzz, 0.7f);
        }

        public void Win()
        {
            if (!On()) return;
            effects.pitch = 1f;
            effects.PlayOneShot(win, 0.85f);
        }

        bool On()
        {
            return settings != null && settings.sound;
        }

        static AudioClip Tone(float freq, float duration, float decay)
        {
            int rate = 44100;
            int count = Mathf.Max(1, (int)(rate * duration));
            float[] data = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)rate;
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * Mathf.Exp(-decay * t) * 0.45f;
            }
            return Clip("tone", data, rate);
        }

        static AudioClip BuzzClip()
        {
            int rate = 44100;
            int count = (int)(rate * 0.18f);
            float[] data = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)rate;
                float env = Mathf.Exp(-8f * t);
                float wave = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * 110f * t));
                data[i] = wave * env * 0.2f;
            }
            return Clip("buzz", data, rate);
        }

        static AudioClip Arpeggio()
        {
            float[] notes = { 523.25f, 659.25f, 783.99f, 1046.5f };
            int rate = 44100;
            int count = (int)(rate * 0.55f);
            float[] data = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)rate;
                int step = Mathf.Clamp((int)(t / 0.12f), 0, notes.Length - 1);
                float local = t - step * 0.12f;
                data[i] = Mathf.Sin(2f * Mathf.PI * notes[step] * t) * Mathf.Exp(-10f * local) * 0.4f;
            }
            return Clip("win", data, rate);
        }

        static AudioClip Pad()
        {
            int rate = 44100;
            int count = rate * 4;
            float[] data = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)rate;
                float a = Mathf.Sin(2f * Mathf.PI * 196f * t);
                float b = Mathf.Sin(2f * Mathf.PI * 246.94f * t);
                data[i] = (a + b) * 0.08f;
            }
            return Clip("pad", data, rate);
        }

        static AudioClip Clip(string name, float[] data, int rate)
        {
            AudioClip clip = AudioClip.Create(name, data.Length, 1, rate, false);
            clip.SetData(data, 0);
            clip.hideFlags = HideFlags.HideAndDontSave;
            return clip;
        }
    }
}
