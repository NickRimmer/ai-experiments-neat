﻿using Neat.Core.Phenotypes;
using Neat.Trainer.Simulations.FieldRunner.Enums;
namespace Neat.Trainer.Simulations.FieldRunner.Models;

public record WorldItem(WorldItemType Type);

public record PikaWorldItem() : WorldItem(WorldItemType.Pika)
{
    public required int Energy { get; init; }
    public required Direction Direction { get; init; }
    public required PhenotypeRunner PhenotypeRunner { get; init; }

    public PikaWorldItem Duplicate() => new ()
    {
        Energy = Energy,
        Direction = Direction,
        PhenotypeRunner = PhenotypeRunner, // do not duplicate, but reference. It can have recurrent neurons memory
    };
}
