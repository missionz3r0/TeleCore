﻿using System.Collections.Generic;
using System.Linq;
using Verse;

namespace TeleCore;

public struct NetworkGraphPathResult
{
    public readonly NetworkGraphPathRequest request;
    public readonly INetworkSubPart[][] allPaths;
    public readonly HashSet<INetworkSubPart> allPartsUnique;
    public readonly HashSet<INetworkSubPart> allTargets;

    //
    public readonly INetworkSubPart[] singlePath;
 
    public bool IsValid => allTargets != null && allTargets.Any();

    public static NetworkGraphPathResult Invalid => new NetworkGraphPathResult()
    {
    };

    public NetworkGraphPathResult(NetworkGraphPathRequest request, List<List<INetworkSubPart>> allResults)
    {
        TLog.Debug($"Request - {allResults.Count}");
        this.request = request;
        allPaths = new INetworkSubPart[allResults.Count][];
        allPartsUnique = new();
        allTargets = new HashSet<INetworkSubPart>();
        singlePath = allPaths.First();
        for (var i = 0; i < allResults.Count; i++)
        {
            allPaths[i] = allResults[i].ToArray();
            allPartsUnique.AddRange(allPaths[i]);
            allTargets.Add(allPaths[i].Last());
        }
    }

    public NetworkGraphPathResult(NetworkGraphPathRequest request, List<INetworkSubPart> result)
    {
        this.request = request;
        singlePath = result.ToArray();
        
        //
        allPaths = new INetworkSubPart[1][] {singlePath};
        allPartsUnique = new HashSet<INetworkSubPart>();
        allPartsUnique.AddRange(result);

        allTargets = new HashSet<INetworkSubPart>() { result.Last() };
        singlePath = null;
    }
}