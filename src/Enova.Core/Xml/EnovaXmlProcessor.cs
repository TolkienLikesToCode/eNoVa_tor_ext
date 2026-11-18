using System.Xml;
using Lragrz.Enova.Core.Crypto;
using Lragrz.Enova.Core.Models;
using Microsoft.Extensions.Logging;

namespace Lragrz.Enova.Core.Xml;

/// <summary>
/// Pure in-memory processor: Load → answer transform → optional QES.
/// </summary>
public sealed class EnovaXmlProcessor
{
    private readonly IQesProvider _qesProvider;
    private readonly ILogger<EnovaXmlProcessor> _logger;
    private readonly string _senderName;
    private readonly bool _signingEnabled;

    public EnovaXmlProcessor(
        IQesProvider qesProvider,
        ILogger<EnovaXmlProcessor> logger,
        string senderName = "LH München",
        bool signingEnabled = false)
    {
        _qesProvider = qesProvider ?? throw new ArgumentNullException(nameof(qesProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _senderName = senderName;
        _signingEnabled = signingEnabled;
    }

    public XmlDocument Load(Stream inputStream)
    {
        ArgumentNullException.ThrowIfNull(inputStream);
        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            IgnoreComments = false,
            IgnoreWhitespace = false
        };
        using var reader = XmlReader.Create(inputStream, settings);
        var document = new XmlDocument { PreserveWhitespace = true };
        document.Load(reader);
        _logger.LogDebug("XML loaded. Root = {Root}", document.DocumentElement?.LocalName);
        return document;
    }

    public XmlDocument LoadFromFile(string path)
    {
        using var fs = File.OpenRead(path);
        return Load(fs);
    }

    public async Task<ProcessingResult> ProcessAsync(
        Stream inputStream,
        AnswerParameters? answerParameters = null,
        CancellationToken cancellationToken = default)
    {
        var document = Load(inputStream);
        ErsuchenSachentscheidungChecker.Check(document);

        if (answerParameters is not null)
        {
            AnswerTransform.Apply(document, answerParameters, _senderName);
            _logger.LogInformation("Answer transform applied.");
        }

        if (!_signingEnabled)
        {
            _logger.LogInformation("Signing disabled – pure answer document.");
            return new ProcessingResult(document, "NONE");
        }

        var signed = await _qesProvider
            .SignEnvelopedAsync(document, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        return new ProcessingResult(signed, _qesProvider.ProviderId);
    }

    public async Task<ProcessingResult> ProcessFileAsync(
        string inputPath,
        AnswerParameters? answerParameters = null,
        CancellationToken cancellationToken = default)
    {
        await using var fs = File.OpenRead(inputPath);
        return await ProcessAsync(fs, answerParameters, cancellationToken).ConfigureAwait(false);
    }
}
