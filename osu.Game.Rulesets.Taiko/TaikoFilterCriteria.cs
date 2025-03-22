// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Bindables;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Filter;
using osu.Game.Rulesets.Mods;
using osu.Game.Screens.Select;
using osu.Game.Screens.Select.Filter;

namespace osu.Game.Rulesets.Taiko
{
    public class TaikoFilterCriteria : IRulesetFilterCriteria
    {
        public bool Matches(BeatmapInfo beatmapInfo, FilterCriteria criteria)
        {
            // Taiko is very well convertible, surprisingly even with CtB.
            // However, Mania conversion currently still needs some work since
            // the converted maps often have way too many spinners.
            return beatmapInfo.Ruleset.ShortName == "taiko"
                || (criteria.AllowConvertedBeatmaps && (
                    beatmapInfo.Ruleset.ShortName == "osu" ||
                    beatmapInfo.Ruleset.ShortName == "fruits"));
        }

        public bool TryParseCustomKeywordCriteria(string key, Operator op, string value)
        {
            return false;
        }

        public bool FilterMayChangeFromMods(ValueChangedEvent<IReadOnlyList<Mod>> mods)
        {
            return false;
        }
    }
}
