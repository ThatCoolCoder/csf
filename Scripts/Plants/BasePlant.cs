using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;

public class BasePlant : Spatial, ITickable
{
    public class GrowthStage
    {
        public int DurationTicks; // Duration of this growth stage in ticks
        public List<string> VisibleNodes; // Path to models/whatever representing this stage of growth
        public GrowthStage(int durationTicks, string visibleNode)
        {
            DurationTicks = durationTicks;
            VisibleNodes = new() {visibleNode};
        }
        public GrowthStage(int durationTicks, List<string> visibleNodes)
        {
            DurationTicks = durationTicks;
            VisibleNodes = visibleNodes;
        }

        public void Show(Node parent)
        {
            VisibleNodes.ForEach(x => parent.GetNode<Spatial>(x).Show());
        }

        public void Hide(Node parent)
        {
            VisibleNodes.ForEach(x => parent.GetNode<Spatial>(x).Hide());
        }
    }

    public virtual List<GrowthStage> GrowthStages { get; set; } = new();
    
    [PersistableProperty]
    public int CurrentGrowthStageTicks { get; set; } = 0;

    [PersistableProperty]
    public int CurrentGrowthStageIdx
    {
        get { return GrowthStages.IndexOf(currentGrowthStage); }
        set { currentGrowthStage = GrowthStages[value]; }
    }
    private GrowthStage currentGrowthStage;

    public override void _Ready()
    {
        GrowthStages.ForEach(stage => stage.Hide(this));
        if (currentGrowthStage == null && GrowthStages.Count != 0) SetGrowthStage(GrowthStages[0]);
        else if (currentGrowthStage != null) SetGrowthStage(currentGrowthStage);
    }

    public void SubTick()
    {
        
    }

    public void Tick()
    {
        CurrentGrowthStageTicks += 1;
        if (currentGrowthStage != null && CurrentGrowthStageTicks == currentGrowthStage.DurationTicks + 1) // add one because the first tick wasn't a full tick probably
        {
            int index = GrowthStages.IndexOf(currentGrowthStage);
            if (index + 1 < GrowthStages.Count) SetGrowthStage(GrowthStages[index + 1]);
        }
    }

    private void SetGrowthStage(GrowthStage stage)
    {
        if (currentGrowthStage != null) currentGrowthStage.Hide(this);
        currentGrowthStage = stage;
        CurrentGrowthStageTicks = 0;
        currentGrowthStage.Show(this);
    }
}