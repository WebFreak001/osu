// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Bindables;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Filter;
using osu.Game.Rulesets.Mods;
using osu.Game.Screens.Select;
using osu.Game.Screens.Select.Filter;

namespace osu.Game.Rulesets.Catch
{
    public class CatchFilterCriteria : IRulesetFilterCriteria
    {
        public bool Matches(BeatmapInfo beatmapInfo, FilterCriteria criteria)
        {
            return beatmapInfo.Ruleset.ShortName == "fruits"
                || (criteria.AllowConvertedBeatmaps && (
                    beatmapInfo.Ruleset.ShortName == "osu"));
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
