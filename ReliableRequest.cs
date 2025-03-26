using System.Threading;

namespace ReliableRequestLib;

public class ReliableRequest
{
    private readonly string _url;
    private readonly int _maxAttempts;
    private readonly HttpClient _httpClient;


    public ReliableRequest(string url, int timeout, int maxAttempts)
    {
        if(string.IsNullOrWhiteSpace(url))
            throw new ArgumentNullException("url не может быть пустой!", nameof(url));
        if (timeout <= 0)
            throw new ArgumentOutOfRangeException("Timeout должен быть положительным числом.", nameof(timeout));
        if(maxAttempts <=0)
            throw new ArgumentOutOfRangeException("Количество попыток должно быть больше 0",nameof(maxAttempts));

        _url = url;
        _maxAttempts = maxAttempts;

        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromMicroseconds(timeout)
        };
    }


    public async Task<bool> PostAsync(string data, HttpRequestMessage message = null, Action<string> logs = null)
    {
        for (var attempt = 1; attempt <= _maxAttempts; attempt++)
        {
            try
            {
                HttpRequestMessage request;

                if (message != null)
                {
                    request = new HttpRequestMessage(HttpMethod.Post, _url);

                    foreach (var header in message.Headers)
                    {
                        request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                    }

                    request.Content = message.Content ?? new StringContent(data);
                }
                else
                {
                    request = new HttpRequestMessage(HttpMethod.Post, _url)
                    {
                        Content = new StringContent(data)
                    };

                    var response = await _httpClient.SendAsync(request);

                    return response.IsSuccessStatusCode;
                }
            }

            catch (Exception ex)
            {
                if (logs != null)
                    logs(ex.Message);
            }

            if (attempt >= _maxAttempts) continue;

            logs($"Попытка отправить данные {attempt}");
            await Task.Delay(250);
        }

        return false;
    }

    public async Task<bool> GetAsync(HttpRequestMessage message = null, Action<string> logs = null)
    {
        for (var attempt = 1; attempt <= _maxAttempts; attempt++)
        {
            try
            {
                HttpRequestMessage request;
                if (message != null)
                {
                    request = new HttpRequestMessage(HttpMethod.Get, _url);

                    foreach (var header in message.Headers)
                    {
                        request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                    }
                }
                else
                {
                    request = new HttpRequestMessage(HttpMethod.Get, _url);
                }

                var response = await _httpClient.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                if (logs != null)
                    logs(ex.Message);
            }

            if (attempt >= _maxAttempts) continue;
            logs($"Попытка отправить данные {attempt}");
            await Task.Delay(250);

        }
    }
}

