namespace ReliableRequestLib;

public class ReliableRequest
{
    private readonly HttpClient _httpClient = new();

    public async Task<bool> PostAsync(string url, int timeout, int maxAttempts, string data = "", HttpRequestMessage message = null, Action<string> logs = null)
    {
        if (!CheckValidParameters(url, timeout, maxAttempts, logs))
            return false;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                using var cts = new CancellationTokenSource(timeout);
                using var request = message != null ? CloneRequest(message, url) : new HttpRequestMessage(HttpMethod.Post, url);

                if (!string.IsNullOrEmpty(data) && request.Content == null)
                {
                    request.Content = new StringContent(data);
                }

                var response = await _httpClient.SendAsync(request, cts.Token);
                return response.IsSuccessStatusCode;
            }
            catch (TaskCanceledException)
            {
                logs?.Invoke($"Запрос превысил таймаут {timeout} мс.");
            }
            catch (Exception ex)
            {
                logs?.Invoke($"Ошибка: {ex.Message}");
            }

            if (attempt < maxAttempts)
            {
                logs?.Invoke($"Попытка {attempt} не удалась, повтор через 250 мс...");
                await Task.Delay(250);
            }
        }

        return false;
    }

    public async Task<bool> GetAsync(string url, int timeout, int maxAttempts, HttpRequestMessage message = null, Action<string> logs = null)
    {
        var response = await _SendRequestAsync(url, timeout, maxAttempts, message, logs);
        return response?.IsSuccessStatusCode ?? false;
    }

    public async Task<byte[]> GetAsyncBytes(string url, int timeout, int maxAttempts, HttpRequestMessage message = null, Action<string> logs = null)
    {
        var response = await _SendRequestAsync(url, timeout, maxAttempts, message, logs);
        return response != null && response.IsSuccessStatusCode ? await response.Content.ReadAsByteArrayAsync() : null;
    }

    private async Task<HttpResponseMessage> _SendRequestAsync(string url, int timeout, int maxAttempts, HttpRequestMessage message, Action<string> logs)
    {
        if (!CheckValidParameters(url, timeout, maxAttempts, logs))
            return null;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                using var cts = new CancellationTokenSource(timeout);
                using var request = message != null ? CloneRequest(message, url) : new HttpRequestMessage(HttpMethod.Get, url);

                var response = await _httpClient.SendAsync(request, cts.Token);
                if (response.IsSuccessStatusCode)
                    return response;
            }
            catch (TaskCanceledException)
            {
                logs?.Invoke($"Запрос превысил таймаут {timeout} мс.");
            }
            catch (Exception ex)
            {
                logs?.Invoke($"Ошибка: {ex.Message}");
            }

            if (attempt < maxAttempts)
            {
                logs?.Invoke($"Попытка {attempt} не удалась, повтор через 250 мс...");
                await Task.Delay(250);
            }
        }

        return null;
    }

    private bool CheckValidParameters(string url, int timeout, int maxAttempts, Action<string> logs = null)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            var message = "Url не может быть пустым!";
            if (logs == null) throw new ArgumentNullException(nameof(url), message);
            logs(message);
            return false;
        }

        if (timeout <= 0)
        {
            var message = "Timeout должен быть положительным числом.";
            if (logs == null) throw new ArgumentOutOfRangeException(nameof(timeout), message);
            logs(message);
            return false;
        }

        if (maxAttempts <= 0)
        {
            var message = "Количество попыток должно быть больше 0.";
            if (logs == null) throw new ArgumentOutOfRangeException(nameof(maxAttempts), message);
            logs(message);
            return false;
        }

        return true;
    }

    private HttpRequestMessage CloneRequest(HttpRequestMessage original, string url)
    {
        var request = new HttpRequestMessage(original.Method, url);
        foreach (var header in original.Headers)
        {
            request.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        if (original.Content != null)
        {
            request.Content = new StringContent(original.Content.ReadAsStringAsync().Result);
        }

        return request;
    }
}