using Rhinox.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestConfig : LoadableConfigFile<TestConfig, ConfigFileIniLoader>
{
    [ConfigSection("core")]
    [ConfigCommandArg(nameof(floaty))]
    public float floaty;
    [ConfigSection("core")]
    [ConfigCommandArg(nameof(wholeNumb))]
    public int wholeNumb;
    [ConfigSection("core")]
    [ConfigCommandArg(nameof(boolean))]
    public bool boolean;
    [ConfigSection("core")]
    [ConfigCommandArg(nameof(myName))]
    public string myName;

    public override string RelativeFilePath => "config.ini";
}