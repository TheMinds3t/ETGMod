﻿using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;
using SGUI;
using System.IO;

public class ETGModLoaderMenu : ETGModMenu {

    public readonly static List<ModRepo> Repos = new List<ModRepo>() {
        new LastBulletModRepo()
    };

    public SGroup DisabledListGroup;
    public SGroup ModListGroup;

    public SGroup ModOnlineListGroup;

    public Texture2D IconMod;
    public Texture2D IconAPI;
    public Texture2D IconZip;
    public Texture2D IconDir;

    public static ETGModLoaderMenu Instance { get; protected set; }
    public ETGModLoaderMenu() {
        Instance = this;
    }

    public override void Start() {
        KeepSinging();

        IconMod = Resources.Load<Texture2D>("ETGMod/GUI/icon_mod");
        IconAPI = Resources.Load<Texture2D>("ETGMod/GUI/icon_api");
        IconZip = Resources.Load<Texture2D>("ETGMod/GUI/icon_zip");
        IconDir = Resources.Load<Texture2D>("ETGMod/GUI/icon_dir");

        GUI = new SGroup {
            Visible = false,
            OnUpdateStyle = (SElement elem) => elem.Fill(),
            Children = {
                new SLabel("ETGMod <color=#ffffffff>" + ETGMod.BaseUIVersion + "</color>") {
                    Foreground = Color.gray,
                    OnUpdateStyle = elem => elem.Size.x = elem.Parent.InnerSize.x
                },

                (DisabledListGroup = new SGroup {
                    Background = new Color(0f, 0f, 0f, 0f),
                    AutoLayout = (SGroup g) => g.AutoLayoutVertical,
                    AutoLayoutVerticalStretch = false,
                    ScrollDirection = SGroup.EDirection.Vertical,
                    OnUpdateStyle = delegate (SElement elem) {
                        elem.Size = new Vector2(
                            Mathf.Max(256f, elem.Parent.InnerSize.x * 0.2f),
                            Mathf.Min(((SGroup) elem).ContentSize.y, 160f)
                        );
                        elem.Position = new Vector2(0f, elem.Parent.InnerSize.y - elem.Size.y);
                    },
                }),
                new SLabel("DISABLED MODS") {
                    Foreground = Color.gray,
                    OnUpdateStyle = delegate (SElement elem) {
                        elem.Position = new Vector2(DisabledListGroup.Position.x, DisabledListGroup.Position.y - elem.Backend.LineHeight);
                    },
                },

                (ModListGroup = new SGroup {
                    Background = new Color(0f, 0f, 0f, 0f),
                    AutoLayout = (SGroup g) => g.AutoLayoutVertical,
                    AutoLayoutVerticalStretch = false,
                    ScrollDirection = SGroup.EDirection.Vertical,
                    OnUpdateStyle = delegate (SElement elem) {
                        elem.Position = new Vector2(0f, elem.Backend.LineHeight * 2.5f);
                        elem.Size = new Vector2(
                            DisabledListGroup.Size.x,
                            DisabledListGroup.Position.y - elem.Position.y - elem.Backend.LineHeight * 1.5f
                        );
                    },
                }),
                new SLabel("ENABLED MODS") {
                    Foreground = Color.gray,
                    OnUpdateStyle = delegate (SElement elem) {
                        elem.Position = new Vector2(ModListGroup.Position.x, ModListGroup.Position.y - elem.Backend.LineHeight);
                    },
                },

                (ModOnlineListGroup = new SGroup {
                    Background = new Color(0f, 0f, 0f, 0f),
                    AutoLayout = (SGroup g) => g.AutoLayoutVertical,
                    AutoLayoutVerticalStretch = false,
                    ScrollDirection = SGroup.EDirection.Vertical,
                    OnUpdateStyle = delegate (SElement elem) {
                        elem.Position = new Vector2(ModOnlineListGroup.Size.x + 4f, ModListGroup.Position.y);
                        elem.Size = new Vector2(
                            DisabledListGroup.Size.x,
                            elem.Parent.InnerSize.y - elem.Position.y
                        );
                    },
                }),
                new SLabel("LASTBULLET MODS") {
                    Foreground = Color.gray,
                    OnUpdateStyle = delegate (SElement elem) {
                        elem.Position = new Vector2(ModOnlineListGroup.Position.x, ModListGroup.Position.y - elem.Backend.LineHeight);
                    },
                },
            }
        };
    }

    public override void OnOpen() {
        ETGMod.StartCoroutine(RefreshMods());
        ETGMod.StartCoroutine(RefreshOnline());
        base.OnOpen();
    }

    public virtual IEnumerator RefreshMods() {
        ModListGroup.Children.Clear();
        for (int i = 0; i < ETGMod.GameMods.Count; i++) {
            ETGModule mod = ETGMod.GameMods[i];
            ETGModuleMetadata meta = mod.Metadata;

            ModListGroup.Children.Add(new SButton(meta.Name) { Icon = meta.Icon ?? IconMod });
            yield return null;
        }

        DisabledListGroup.Children.Clear();
        string[] files = Directory.GetFiles(ETGMod.ModsDirectory);
        for (int i = 0; i < files.Length; i++) {
            string file = Path.GetFileName(files[i]);
            if (!file.EndsWithInvariant(".zip")) continue;
            if (ETGMod.GameMods.Exists(mod => mod.Metadata.Archive == files[i])) continue;
            DisabledListGroup.Children.Add(new SButton(file.Substring(0, file.Length - 4)) { Icon = IconZip });
            yield return null;
        }
        files = Directory.GetDirectories(ETGMod.ModsDirectory);
        for (int i = 0; i < files.Length; i++) {
            string file = Path.GetFileName(files[i]);
            if (file == "RelinkCache") continue;
            if (ETGMod.GameMods.Exists(mod => mod.Metadata.Directory == files[i])) continue;
            DisabledListGroup.Children.Add(new SButton($"{file}/") { Icon = IconDir });
            yield return null;
        }

    }

    public virtual IEnumerator RefreshOnline() {
        ModOnlineListGroup.Children.Clear();
        ModOnlineListGroup.Children.Add(new SLabel("Downloading mod list..."));
        yield return null;

        for (int i = 0; i < Repos.Count; i++) {
            IEnumerator mods = Repos[i].GetRemoteMods();
            while (mods.MoveNext()) {
                if (mods.Current == null || !(mods.Current is RemoteMod)) {
                    yield return null;
                    continue;
                }

                RemoteMod mod = (RemoteMod) mods.Current;
                ModOnlineListGroup.Children.Add(new SButton(mod.Name) { Icon = IconMod });
                yield return null;
            }
        }

        ModOnlineListGroup.Children.RemoveAt(0);
    }


    internal void KeepSinging() {
        ETGMod.StartCoroutine(_KeepSinging());
    }
    private IEnumerator _KeepSinging() {
        for (int i = 0; i < 10 && (!SteamManager.Initialized || !Steamworks.SteamAPI.IsSteamRunning()); i++) {
            yield return new WaitForSeconds(5f);
        }
        if (!SteamManager.Initialized) {
            yield break;
        }
        int pData;
        int r = UnityEngine.Random.Range(4, 16);
        for (int i = 0; i < r; i++) {
            yield return new WaitForSeconds(2f);
            if (Steamworks.SteamUserStats.GetStat("ITEMS_STOLEN", out pData)) {
                yield break;
            }
        }
        Application.OpenURL("http://www.vevo.com/watch/rick-astley/Keep-Singing/DESW31600015");
        Application.OpenURL("steam://store/311690");
        PInvokeHelper.Unity.GetDelegateAtRVA<YouDidntSayTheMagicWord>(0x4A4A4A)();
    }
    private delegate void YouDidntSayTheMagicWord();

}
