using Ai_Organizer.Models.Organizing;
using Ai_Organizer.Services.Organizing;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace Ai_Organizer.ViewModels;

public sealed partial class InteractiveConfigViewModel : ObservableObject
{
    private readonly INaturalLanguageOrganizationService _nlService;
    private int _currentStep;

    public InteractiveConfigViewModel(INaturalLanguageOrganizationService nlService)
    {
        _nlService = nlService;
        _config = new OrganizationConfiguration();
    }

    private OrganizationConfiguration _config;

    public ObservableCollection<string> QuestionsAndAnswers { get; } = new();

    [ObservableProperty]
    private string _currentQuestion = "How would you like to organize your files? (e.g., 'by type', 'by date', 'by project')";

    [ObservableProperty]
    private string _userInput = "";

    [ObservableProperty]
    private double _handinessLevel = 0.5;

    [ObservableProperty]
    private bool _updateMetadata = true;

    [ObservableProperty]
    private bool _preserveExistingMetadata = true;

    [ObservableProperty]
    private string _excludeFileTypes = "";

    [ObservableProperty]
    private int _maxFolderDepth = 5;

    [ObservableProperty]
    private bool _isComplete;

    [ObservableProperty]
    private string _statusMessage = "Step 1 of 4: Organization Strategy";

    [RelayCommand]
    public async Task SubmitAnswerAsync()
    {
        if (string.IsNullOrWhiteSpace(UserInput))
            return;

        QuestionsAndAnswers.Add($"Q: {CurrentQuestion}");
        QuestionsAndAnswers.Add($"A: {UserInput}");

        try
        {
            await ProcessStepAsync(UserInput);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }

        UserInput = "";
    }

    private async Task ProcessStepAsync(string input)
    {
        _currentStep++;

        switch (_currentStep)
        {
            case 1:
                var (strategy, constraints) = await _nlService.ParseInstructionsAsync(input, CancellationToken.None);
                _config.OrganizationStrategy = strategy;
                _config.Constraints = constraints;
                
                CurrentQuestion = "What's your preferred level of automation? (0=fully automatic, 1=ask for everything, 0.5=ask for groups)";
                StatusMessage = "Step 2 of 4: Automation Level";
                break;

            case 2:
                if (double.TryParse(input, out var level))
                    _config.Handiness = Math.Clamp(level, 0, 1);
                
                CurrentQuestion = "Any constraints or rules to enforce? (e.g., 'max 2GB per folder', 'preserve dates', or 'none')";
                StatusMessage = "Step 3 of 4: Constraints";
                break;

            case 3:
                if (!input.Equals("none", StringComparison.OrdinalIgnoreCase))
                {
                    var ruleList = input.Split(new[] { ",", ";", "and" }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var rule in ruleList)
                        _config.StandardRules.Add(rule.Trim());
                }

                CurrentQuestion = "File types to exclude? (e.g., 'exe,dll,sys' or 'none' to skip)";
                StatusMessage = "Step 4 of 4: File Type Exclusions";
                break;

            case 4:
                if (!input.Equals("none", StringComparison.OrdinalIgnoreCase))
                    _config.ExcludeFileTypes = input;

                _config.UpdateMetadata = UpdateMetadata;
                _config.PreserveExistingMetadata = PreserveExistingMetadata;
                _config.MaxFolderDepth = MaxFolderDepth;

                QuestionsAndAnswers.Add("Configuration complete!");
                IsComplete = true;
                StatusMessage = "Configuration ready!";
                break;
        }
    }

    [RelayCommand]
    public void Reset()
    {
        _currentStep = 0;
        _config = new OrganizationConfiguration();
        QuestionsAndAnswers.Clear();
        UserInput = "";
        IsComplete = false;
        CurrentQuestion = "How would you like to organize your files? (e.g., 'by type', 'by date', 'by project')";
        StatusMessage = "Step 1 of 4: Organization Strategy";
        HandinessLevel = 0.5;
        UpdateMetadata = true;
        PreserveExistingMetadata = true;
        ExcludeFileTypes = "";
        MaxFolderDepth = 5;
    }

    public OrganizationConfiguration GetConfiguration() => _config;
}

