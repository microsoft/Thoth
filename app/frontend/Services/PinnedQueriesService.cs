// Copyright (c) Microsoft. All rights reserved.

namespace ClientApp.Services;
public class PinnedQueriesService
{
    private const string PINNED_QUERIES_KEY = "pinnedQueries";

    private readonly Dictionary<int, UserQuestion> _pinnedQueries = new();

    public event Action OnChange;
    public List<EventCallback> listeners { get; private set; } = new List<EventCallback>();
    private void NotifyStateChanged() => OnChange?.Invoke();

    public PinnedQueriesService()
    {
        _pinnedQueries.Add(1, new UserQuestion("What is the weather today?", DateTime.Now));
        _pinnedQueries.Add(2, new UserQuestion("What time is it when an elephant sits on your watch?", DateTime.Now.AddMinutes(1)));
        _pinnedQueries.Add(3, new UserQuestion("What is the meaning of life?", DateTime.Now.AddMinutes(2)));
        _pinnedQueries.Add(4, new UserQuestion("How much wood could a wood chuck chuck if a wood chuck could chuck wood?", DateTime.Now.AddMinutes(3)));
    }

    // todo: refactor need to be consistent about using a key or letting the question be the key

    public bool TryGetUserQuestion(int questionId, out UserQuestion userQuestion)
    {
        return _pinnedQueries.TryGetValue(questionId, out userQuestion);
    }

    public IEnumerable<UserQuestion> GetPinnedQueries()
    {
        return _pinnedQueries.Values;
    }

    public int AddPinnedQuery(UserQuestion userQuestion)
    {
        var queryId = _pinnedQueries.Keys.Any() ? _pinnedQueries.Keys.Max() + 1 : 1;
        _pinnedQueries.Add(queryId, userQuestion);
        NotifyStateChanged();
        return queryId;
    }

    public void DeletePinnedQuery(string question)
    {
        var query = _pinnedQueries.FirstOrDefault(kvp => string.Equals(kvp.Value.Question, question, StringComparison.InvariantCultureIgnoreCase));
        _pinnedQueries.Remove(query.Key);
        NotifyStateChanged();
    }

    // todo: update?
}
