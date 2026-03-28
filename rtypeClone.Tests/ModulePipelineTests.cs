using rtypeClone.Systems.ModuleSystem;

namespace rtypeClone.Tests;

public class ModulePipelineTests
{
    private static ModuleDefinition MakeWeapon(
        int damage = 1, float speed = 800f, float width = 12f, float height = 4f,
        string[]? tags = null)
    {
        return new ModuleDefinition
        {
            Id = "test_weapon",
            DisplayName = "Test Weapon",
            Category = ModuleCategory.Weapon,
            WeaponCategory = Systems.ModuleSystem.WeaponCategory.Shot,
            BaseProjectileParameters = new ProjectileParameters
            {
                Damage = damage, Speed = speed, Width = width, Height = height,
                Count = 1, RadiusMultiplier = 1f
            },
            Tags = tags ?? ["projectile"]
        };
    }

    private static ModuleDefinition MakeSupport(
        ModuleModifiers mods, string[]? requiresTags = null)
    {
        return new ModuleDefinition
        {
            Id = "test_support",
            DisplayName = "Test Support",
            Category = ModuleCategory.Support,
            Modifiers = mods,
            RequiresTags = requiresTags ?? []
        };
    }

    [Fact]
    public void Resolve_NoSupports_ReturnsBase()
    {
        var weapon = MakeWeapon(damage: 5, speed: 600f);
        var result = ModulePipeline.Resolve(weapon, ReadOnlySpan<ModuleDefinition?>.Empty);

        Assert.Equal(5, result.Damage);
        Assert.Equal(600f, result.Speed);
    }

    [Fact]
    public void Resolve_DamageFlat_AddsDamage()
    {
        var weapon = MakeWeapon(damage: 1);
        var support = MakeSupport(new ModuleModifiers
        {
            DamageFlat = 2, DamageMultiplier = 1f, RadiusMultiplier = 1f
        });
        var supports = new ModuleDefinition?[] { support };

        var result = ModulePipeline.Resolve(weapon, supports);
        Assert.Equal(3, result.Damage); // 1 + 2 = 3
    }

    [Fact]
    public void Resolve_DamageMultiplier_MultipliesAfterFlat()
    {
        var weapon = MakeWeapon(damage: 1);
        var support = MakeSupport(new ModuleModifiers
        {
            DamageFlat = 1, DamageMultiplier = 1.5f, RadiusMultiplier = 1f
        });
        var supports = new ModuleDefinition?[] { support };

        var result = ModulePipeline.Resolve(weapon, supports);
        Assert.Equal(3, result.Damage); // (1 + 1) * 1.5 = 3
    }

    [Fact]
    public void Resolve_Pierce_AddsDelta()
    {
        var weapon = MakeWeapon();
        var support = MakeSupport(new ModuleModifiers
        {
            PierceDelta = 3, DamageMultiplier = 1f, RadiusMultiplier = 1f
        });
        var supports = new ModuleDefinition?[] { support };

        var result = ModulePipeline.Resolve(weapon, supports);
        Assert.Equal(3, result.Pierce);
    }

    [Fact]
    public void Resolve_SpeedFlat_AddsSpeed()
    {
        var weapon = MakeWeapon(speed: 800f);
        var support = MakeSupport(new ModuleModifiers
        {
            SpeedFlat = 300f, DamageMultiplier = 1f, RadiusMultiplier = 1f
        });
        var supports = new ModuleDefinition?[] { support };

        var result = ModulePipeline.Resolve(weapon, supports);
        Assert.Equal(1100f, result.Speed);
    }

    [Fact]
    public void Resolve_Multishot_AddsCountAndSpread()
    {
        var weapon = MakeWeapon();
        var support = MakeSupport(new ModuleModifiers
        {
            CountDelta = 2, SpreadAngleDeg = 15f,
            DamageMultiplier = 1f, RadiusMultiplier = 1f
        });
        var supports = new ModuleDefinition?[] { support };

        var result = ModulePipeline.Resolve(weapon, supports);
        Assert.Equal(3, result.Count); // 1 + 2
        Assert.Equal(15f, result.SpreadAngleDeg);
    }

    [Fact]
    public void Resolve_Homing_OverridesToTrue()
    {
        var weapon = MakeWeapon();
        var support = MakeSupport(new ModuleModifiers
        {
            HomingOverride = true, HomingStrength = 3f,
            DamageMultiplier = 1f, RadiusMultiplier = 1f
        });
        var supports = new ModuleDefinition?[] { support };

        var result = ModulePipeline.Resolve(weapon, supports);
        Assert.True(result.Homing);
        Assert.Equal(3f, result.HomingStrength);
    }

    [Fact]
    public void Resolve_TwoSupports_StackEffects()
    {
        var weapon = MakeWeapon(damage: 1);
        var sup1 = MakeSupport(new ModuleModifiers
        {
            DamageFlat = 1, DamageMultiplier = 1f, RadiusMultiplier = 1f
        });
        var sup2 = MakeSupport(new ModuleModifiers
        {
            DamageFlat = 1, DamageMultiplier = 1f, RadiusMultiplier = 1f
        });
        var supports = new ModuleDefinition?[] { sup1, sup2 };

        var result = ModulePipeline.Resolve(weapon, supports);
        Assert.Equal(3, result.Damage); // 1 + 1 + 1 = 3
    }

    [Fact]
    public void Resolve_NullSupportSlot_Ignored()
    {
        var weapon = MakeWeapon(damage: 1);
        var support = MakeSupport(new ModuleModifiers
        {
            DamageFlat = 5, DamageMultiplier = 1f, RadiusMultiplier = 1f
        });
        var supports = new ModuleDefinition?[] { null, support };

        var result = ModulePipeline.Resolve(weapon, supports);
        Assert.Equal(6, result.Damage); // 1 + 5
    }

    [Fact]
    public void Resolve_TagMismatch_SupportSkipped()
    {
        var weapon = MakeWeapon(damage: 1, tags: ["physical"]);
        var support = MakeSupport(
            new ModuleModifiers { DamageFlat = 10, DamageMultiplier = 1f, RadiusMultiplier = 1f },
            requiresTags: ["energy"] // weapon doesn't have this tag
        );
        var supports = new ModuleDefinition?[] { support };

        var result = ModulePipeline.Resolve(weapon, supports);
        Assert.Equal(1, result.Damage); // Support was skipped
    }

    [Fact]
    public void Resolve_TagMatch_SupportApplied()
    {
        var weapon = MakeWeapon(damage: 1, tags: ["physical", "projectile"]);
        var support = MakeSupport(
            new ModuleModifiers { DamageFlat = 10, DamageMultiplier = 1f, RadiusMultiplier = 1f },
            requiresTags: ["projectile"]
        );
        var supports = new ModuleDefinition?[] { support };

        var result = ModulePipeline.Resolve(weapon, supports);
        Assert.Equal(11, result.Damage);
    }

    [Fact]
    public void Resolve_DamageClampedToMinimum1()
    {
        var weapon = MakeWeapon(damage: 1);
        var support = MakeSupport(new ModuleModifiers
        {
            DamageFlat = -5, DamageMultiplier = 1f, RadiusMultiplier = 1f
        });
        var supports = new ModuleDefinition?[] { support };

        var result = ModulePipeline.Resolve(weapon, supports);
        Assert.Equal(1, result.Damage); // Clamped from -4 to 1
    }

    [Fact]
    public void Resolve_SpeedClampedToMinimum50()
    {
        var weapon = MakeWeapon(speed: 100f);
        var support = MakeSupport(new ModuleModifiers
        {
            SpeedFlat = -200f, DamageMultiplier = 1f, RadiusMultiplier = 1f
        });
        var supports = new ModuleDefinition?[] { support };

        var result = ModulePipeline.Resolve(weapon, supports);
        Assert.Equal(50f, result.Speed); // Clamped from -100 to 50
    }

    [Fact]
    public void ResolveCharged_UsesChargedBase()
    {
        var weapon = new ModuleDefinition
        {
            Id = "test",
            Category = ModuleCategory.Weapon,
            BaseProjectileParameters = new ProjectileParameters { Damage = 1, Speed = 800f, Count = 1, RadiusMultiplier = 1f },
            ChargedProjectileParameters = new ProjectileParameters { Damage = 5, Speed = 400f, Count = 1, RadiusMultiplier = 1f },
            HasChargedMode = true,
            Tags = ["projectile"]
        };

        var result = ModulePipeline.ResolveCharged(weapon, ReadOnlySpan<ModuleDefinition?>.Empty);
        Assert.Equal(5, result.Damage);
        Assert.Equal(400f, result.Speed);
    }
}
