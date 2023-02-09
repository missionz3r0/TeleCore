﻿namespace TeleCore;

public struct FXLayerArgs
{
    public int index;
    public int renderPriority;
    public string layerTag;
    public string categoryTag;
    public FXLayerData data;

    public static implicit operator int(FXLayerArgs args) => args.index;
    public static implicit operator string(FXLayerArgs args) => args.layerTag;
}