using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Flags]
public enum ProcessProgress
{
    Initializing = 0,
    HasSelectedBundle = 2,
    HasSelectedResSA = 4,
    HasSelectedResSB = 8,
    HasCreatedComparator = 16
}