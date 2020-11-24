using System;

namespace DeepDesignLab.Debug {
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class DebugGUIPrintAttribute : Attribute { }
}