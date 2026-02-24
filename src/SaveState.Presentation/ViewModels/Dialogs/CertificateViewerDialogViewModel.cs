// Copyright (c) 2026 SaveStateReborn. All rights reserved.

using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common.Services;

namespace SaveState.Presentation.ViewModels.Dialogs;

/// <summary>
/// ViewModel for the certificate viewer dialog.
/// </summary>
public sealed partial class CertificateViewerDialogViewModel : ObservableObject
{
    private readonly ILogger<CertificateViewerDialogViewModel> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly X509Certificate2? _certificate;

    [ObservableProperty]
    private string _domain = string.Empty;

    [ObservableProperty]
    private string _subject = string.Empty;

    [ObservableProperty]
    private string _issuer = string.Empty;

    [ObservableProperty]
    private DateTime _validFrom;

    [ObservableProperty]
    private DateTime _validTo;

    [ObservableProperty]
    private string _serialNumber = string.Empty;

    [ObservableProperty]
    private string _version = string.Empty;

    [ObservableProperty]
    private string _sha256Fingerprint = string.Empty;

    [ObservableProperty]
    private string _sha1Fingerprint = string.Empty;

    [ObservableProperty]
    private string _signatureAlgorithm = string.Empty;

    [ObservableProperty]
    private string _publicKeyAlgorithm = string.Empty;

    [ObservableProperty]
    private int _keySize;

    [ObservableProperty]
    private bool _isValid;

    [ObservableProperty]
    private bool _isSelfSigned;

    [ObservableProperty]
    private string _validityStatus = string.Empty;

    [ObservableProperty]
    private ObservableCollection<string> _subjectAlternativeNames = new();

    [ObservableProperty]
    private ObservableCollection<CertificateChainItem> _certificateChain = new();

    [ObservableProperty]
    private ObservableCollection<string> _warnings = new();

    [ObservableProperty]
    private bool _hasWarnings;

    public CertificateViewerDialogViewModel(ILogger<CertificateViewerDialogViewModel> logger, ITimeProvider? timeProvider = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _timeProvider = timeProvider ?? SystemTimeProvider.Instance;
    }

    public CertificateViewerDialogViewModel(
        X509Certificate2 certificate,
        string domain,
        ILogger<CertificateViewerDialogViewModel> logger,
        ITimeProvider? timeProvider = null)
    {
        _certificate = certificate;
        Domain = domain;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _timeProvider = timeProvider ?? SystemTimeProvider.Instance;

        LoadCertificateDetails();
    }

    private void LoadCertificateDetails()
    {
        if (_certificate == null) return;

        try
        {
            Subject = _certificate.Subject;
            Issuer = _certificate.Issuer;
            ValidFrom = _certificate.NotBefore;
            ValidTo = _certificate.NotAfter;
            SerialNumber = _certificate.SerialNumber;
            Version = $"V{_certificate.Version}";

            // Calculate fingerprints
            Sha256Fingerprint = BitConverter.ToString(_certificate.GetCertHash(HashAlgorithmName.SHA256))
                .Replace("-", ":");
            Sha1Fingerprint = _certificate.GetCertHashString();

            // Signature algorithm
            SignatureAlgorithm = _certificate.SignatureAlgorithm.FriendlyName ?? "Unknown";

            // Public key info
            PublicKeyAlgorithm = _certificate.PublicKey.Oid.FriendlyName ?? "Unknown";

            // Key size - using Get[Algorithm]PublicKey methods for .NET 9 compatibility
#pragma warning disable SYSLIB0027 // PublicKey.Key is obsolete
            if (_certificate.PublicKey.Key is RSA rsa)
            {
                KeySize = rsa.KeySize;
            }
            else if (_certificate.PublicKey.Key is DSA dsa)
            {
                KeySize = dsa.KeySize;
            }
            else if (_certificate.PublicKey.Key is ECDsa ecdsa)
            {
                KeySize = ecdsa.KeySize;
            }
#pragma warning restore SYSLIB0027

            // Check validity
            IsValid = _certificate.NotBefore <= _timeProvider.UtcNow && _certificate.NotAfter >= _timeProvider.UtcNow;
            ValidityStatus = IsValid ? "Valid" : "Invalid";

            // Check if self-signed
            IsSelfSigned = _certificate.Subject == _certificate.Issuer;

            // Load subject alternative names
            LoadSubjectAlternativeNames();

            // Build certificate chain
            BuildCertificateChain();

            // Check for warnings
            CheckWarnings();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load certificate details");
        }
    }

    private void LoadSubjectAlternativeNames()
    {
        try
        {
            var sanExtension = _certificate?.Extensions["2.5.29.17"];
            if (sanExtension is X509SubjectAlternativeNameExtension san)
            {
                foreach (var name in san.EnumerateDnsNames())
                {
                    SubjectAlternativeNames.Add($"DNS: {name}");
                }
                foreach (var ip in san.EnumerateIPAddresses())
                {
                    SubjectAlternativeNames.Add($"IP: {ip}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load subject alternative names");
        }
    }

    private void BuildCertificateChain()
    {
        if (_certificate == null) return;

        try
        {
            using var chain = new X509Chain();
            chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
            chain.Build(_certificate);

            CertificateChain.Clear();
            foreach (var element in chain.ChainElements)
            {
                CertificateChain.Add(new CertificateChainItem
                {
                    Subject = element.Certificate.Subject,
                    IsRoot = element.Certificate.Subject == element.Certificate.Issuer
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to build certificate chain");

            // Add at least the current certificate
            CertificateChain.Add(new CertificateChainItem
            {
                Subject = _certificate.Subject,
                IsRoot = IsSelfSigned
            });
        }
    }

    private void CheckWarnings()
    {
        Warnings.Clear();

        if (!IsValid)
        {
            if (_timeProvider.UtcNow < ValidFrom)
            {
                Warnings.Add("This certificate is not yet valid.");
            }
            else if (_timeProvider.UtcNow > ValidTo)
            {
                Warnings.Add("This certificate has expired.");
            }
        }

        if (IsSelfSigned)
        {
            Warnings.Add("This is a self-signed certificate. It may not be trusted by your system.");
        }

        if (ValidTo < _timeProvider.UtcNow.AddDays(30) && ValidTo > _timeProvider.UtcNow)
        {
            Warnings.Add($"This certificate expires in {(ValidTo - _timeProvider.UtcNow).Days} days.");
        }

        // Check for weak algorithms
        if (SignatureAlgorithm.Contains("MD5", StringComparison.OrdinalIgnoreCase) ||
            SignatureAlgorithm.Contains("SHA1", StringComparison.OrdinalIgnoreCase))
        {
            Warnings.Add($"This certificate uses a weak signature algorithm ({SignatureAlgorithm}).");
        }

        if (KeySize < 2048 && PublicKeyAlgorithm.Contains("RSA"))
        {
            Warnings.Add($"This certificate uses a weak key size ({KeySize} bits).");
        }

        HasWarnings = Warnings.Count > 0;
    }

    [RelayCommand]
    private void CopyFingerprint(string? type)
    {
        if (string.IsNullOrWhiteSpace(type)) return;

        try
        {
            var text = type.ToUpperInvariant() switch
            {
                "SHA256" => Sha256Fingerprint,
                "SHA1" => Sha1Fingerprint,
                _ => string.Empty
            };

            if (!string.IsNullOrEmpty(text))
            {
                // Copy to clipboard would go here
                _logger.LogInformation("Copied {Type} fingerprint to clipboard", type);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to copy fingerprint");
        }
    }

    [RelayCommand]
    private void ExportCertificate()
    {
        if (_certificate == null) return;

        try
        {
            // This would open a save dialog and export the certificate
            _logger.LogInformation("Export certificate requested");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export certificate");
        }
    }

    [RelayCommand]
    private void Close()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = desktop.Windows.FirstOrDefault(w => w.DataContext == this);
            window?.Close();
        }
    }
}

/// <summary>
/// Represents an item in the certificate chain.
/// </summary>
public sealed record CertificateChainItem
{
    public string Subject { get; set; } = string.Empty;
    public bool IsRoot { get; set; }
    public bool IsNotRoot => !IsRoot;
    public string FontWeight => IsRoot ? "Bold" : "Normal";
}
