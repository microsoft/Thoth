// Copyright (c) Microsoft. All rights reserved.

namespace ClientApp.Services;

public interface ITextToSpeechPreferencesListener
{
    void OnAvailableVoicesChanged(Func<Task> onVoicesChanged);

    void UnsubscribeFromAvailableVoicesChanged();
}
