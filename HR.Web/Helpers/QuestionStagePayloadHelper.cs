using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace HR.Web.Helpers
{
    public static class QuestionStagePayloadHelper
    {
        public static Dictionary<int, HashSet<int>> Parse(string payload, int[] selectedQuestions, int questionnaireStageCount)
        {
            var stages = new Dictionary<int, HashSet<int>>();
            if (selectedQuestions != null)
            {
                foreach (var questionId in selectedQuestions.Distinct())
                {
                    if (questionId > 0)
                    {
                        stages[questionId] = new HashSet<int> { 1 };
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(payload))
            {
                return stages;
            }

            var max = Math.Max(1, questionnaireStageCount);
            foreach (var entry in payload.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = entry.Split('=');
                if (parts.Length != 2)
                {
                    continue;
                }

                int questionId;
                if (!int.TryParse(parts[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out questionId) || questionId <= 0)
                {
                    continue;
                }

                if (!stages.ContainsKey(questionId))
                {
                    continue;
                }

                foreach (var stageToken in parts[1].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    int stage;
                    if (!int.TryParse(stageToken.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out stage))
                    {
                        continue;
                    }

                    stage = Math.Max(1, Math.Min(max, stage));
                    stages[questionId].Add(stage);
                }
            }

            return stages;
        }

        public static string ValidateAllStagesHaveQuestions(int questionnaireStageCount, int[] selectedQuestions, IDictionary<int, HashSet<int>> stages)
        {
            if (questionnaireStageCount <= 1)
            {
                return null;
            }

            var selected = selectedQuestions != null
                ? selectedQuestions.Where(id => id > 0).Distinct().ToList()
                : new List<int>();
            if (!selected.Any())
            {
                return null;
            }

            for (var stage = 1; stage <= questionnaireStageCount; stage++)
            {
                if (!selected.Any(questionId =>
                    stages.ContainsKey(questionId) &&
                    stages[questionId] != null &&
                    stages[questionId].Contains(stage)))
                {
                    return string.Format(
                        "This position uses {0} questionnaire stages. Add at least one question assigned to stage {1}.",
                        questionnaireStageCount,
                        stage);
                }
            }

            return null;
        }

        public static Dictionary<int, IList<int>> ToOrderedLists(IDictionary<int, HashSet<int>> stages)
        {
            if (stages == null)
            {
                return new Dictionary<int, IList<int>>();
            }

            return stages.ToDictionary(
                kvp => kvp.Key,
                kvp => (IList<int>)(kvp.Value != null
                    ? kvp.Value.Where(s => s > 0).OrderBy(s => s).ToList()
                    : new List<int>()));
        }
    }
}
