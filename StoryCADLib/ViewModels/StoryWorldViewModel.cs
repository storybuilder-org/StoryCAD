using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml;
using StoryCADLib.Models;
using StoryCADLib.Models.StoryWorld;
using StoryCADLib.Services;
using StoryCADLib.Services.Collaborator;
using StoryCADLib.Services.Messages;
using StoryCADLib.Services.Navigation;

namespace StoryCADLib.ViewModels;

public class StoryWorldViewModel : ObservableRecipient, INavigable, ISaveable, IReloadable
{
    #region Fields

    private readonly ILogService _logger;
    private readonly AppState _appState;
    // Reserved for future Collaborator AI worldbuilding guidance integration
    // See Collaborator/devdocs/storyworld-ai-parameters-parking.md
    #pragma warning disable IDE0052 // Remove unread private members
    private readonly CollaboratorService _collaboratorService;
    #pragma warning restore IDE0052
    private bool _changeable;
    private bool _changed;

    #endregion

    #region StoryElement Properties

    private Guid _uuid;
    public Guid Uuid
    {
        get => _uuid;
        set => SetProperty(ref _uuid, value);
    }

    private string _name;
    public string Name
    {
        get => _name;
        set
        {
            if (_changeable && _name != value)
            {
                _logger.Log(LogLevel.Info, $"Requesting Name change from {_name} to {value}");
                NameChangeMessage msg = new(_name, value);
                Messenger.Send(new NameChangedMessage(msg));
            }
            SetProperty(ref _name, value);
        }
    }

    private StoryWorldModel _model;
    public StoryWorldModel Model
    {
        get => _model;
        set => _model = value;
    }

    #endregion

    #region Structure Tab Properties

    private string _worldType;
    public string WorldType
    {
        get => _worldType;
        set
        {
            if (SetProperty(ref _worldType, value))
            {
                UpdateWorldTypeDescriptions();
                // Auto-populate axis values when World Type changes
                if (_changeable)
                {
                    AutoPopulateAxisValues();
                }
            }
        }
    }

    private string _ontology;
    public string Ontology
    {
        get => _ontology;
        set => SetProperty(ref _ontology, value);
    }

    private string _worldRelation;
    public string WorldRelation
    {
        get => _worldRelation;
        set => SetProperty(ref _worldRelation, value);
    }

    private string _ruleTransparency;
    public string RuleTransparency
    {
        get => _ruleTransparency;
        set => SetProperty(ref _ruleTransparency, value);
    }

    private string _scaleOfDifference;
    public string ScaleOfDifference
    {
        get => _scaleOfDifference;
        set => SetProperty(ref _scaleOfDifference, value);
    }

    private string _agencySource;
    public string AgencySource
    {
        get => _agencySource;
        set => SetProperty(ref _agencySource, value);
    }

    private string _toneLogic;
    public string ToneLogic
    {
        get => _toneLogic;
        set => SetProperty(ref _toneLogic, value);
    }

    #endregion

    #region List Entry Commands

    #region Physical World Navigation

    // Physical World commands
    public RelayCommand AddPhysicalWorldCommand { get; private set; }
    public RelayCommand RemoveCurrentPhysicalWorldCommand { get; private set; }
    public RelayCommand PreviousPhysicalWorldCommand { get; private set; }
    public RelayCommand NextPhysicalWorldCommand { get; private set; }

    private int _currentPhysicalWorldIndex;
    /// <summary>
    /// Index of the currently displayed Physical World entry.
    /// </summary>
    public int CurrentPhysicalWorldIndex
    {
        get => _currentPhysicalWorldIndex;
        set
        {
            if (SetProperty(ref _currentPhysicalWorldIndex, value))
            {
                NotifyPhysicalWorldNavigationChanged();
            }
        }
    }

    /// <summary>
    /// True if there are any Physical World entries.
    /// </summary>
    public bool HasPhysicalWorlds => PhysicalWorlds?.Count > 0;

    /// <summary>
    /// True if there's a previous entry to navigate to.
    /// </summary>
    public bool HasPreviousPhysicalWorld => CurrentPhysicalWorldIndex > 0;

    /// <summary>
    /// True if there's a next entry to navigate to.
    /// </summary>
    public bool HasNextPhysicalWorld => PhysicalWorlds != null && CurrentPhysicalWorldIndex < PhysicalWorlds.Count - 1;

    /// <summary>
    /// Display string showing position (e.g., "1 of 3" or "0 of 0").
    /// </summary>
    public string PhysicalWorldPositionDisplay =>
        PhysicalWorlds == null || PhysicalWorlds.Count == 0
            ? "0 of 0"
            : $"{CurrentPhysicalWorldIndex + 1} of {PhysicalWorlds.Count}";

    // Current entry property accessors
    public string CurrentPhysicalWorldName
    {
        get => HasPhysicalWorlds && CurrentPhysicalWorldIndex < PhysicalWorlds.Count
            ? PhysicalWorlds[CurrentPhysicalWorldIndex].Name : string.Empty;
        set
        {
            if (HasPhysicalWorlds && CurrentPhysicalWorldIndex < PhysicalWorlds.Count)
            {
                PhysicalWorlds[CurrentPhysicalWorldIndex].Name = value;
                OnPropertyChanged();
                ShellViewModel.ShowChange();
            }
        }
    }

    public string CurrentPhysicalWorldGeography
    {
        get => HasPhysicalWorlds && CurrentPhysicalWorldIndex < PhysicalWorlds.Count
            ? PhysicalWorlds[CurrentPhysicalWorldIndex].Geography : string.Empty;
        set
        {
            if (HasPhysicalWorlds && CurrentPhysicalWorldIndex < PhysicalWorlds.Count)
            {
                PhysicalWorlds[CurrentPhysicalWorldIndex].Geography = value;
                PhysicalWorldGeographyFontWeight = new Windows.UI.Text.FontWeight { Weight = (ushort)(HasRtfContent(value) ? 700 : 400) };
                OnPropertyChanged();
                ShellViewModel.ShowChange();
            }
        }
    }

    public string CurrentPhysicalWorldClimate
    {
        get => HasPhysicalWorlds && CurrentPhysicalWorldIndex < PhysicalWorlds.Count
            ? PhysicalWorlds[CurrentPhysicalWorldIndex].Climate : string.Empty;
        set
        {
            if (HasPhysicalWorlds && CurrentPhysicalWorldIndex < PhysicalWorlds.Count)
            {
                PhysicalWorlds[CurrentPhysicalWorldIndex].Climate = value;
                PhysicalWorldClimateFontWeight = new Windows.UI.Text.FontWeight { Weight = (ushort)(HasRtfContent(value) ? 700 : 400) };
                OnPropertyChanged();
                ShellViewModel.ShowChange();
            }
        }
    }

    public string CurrentPhysicalWorldNaturalResources
    {
        get => HasPhysicalWorlds && CurrentPhysicalWorldIndex < PhysicalWorlds.Count
            ? PhysicalWorlds[CurrentPhysicalWorldIndex].NaturalResources : string.Empty;
        set
        {
            if (HasPhysicalWorlds && CurrentPhysicalWorldIndex < PhysicalWorlds.Count)
            {
                PhysicalWorlds[CurrentPhysicalWorldIndex].NaturalResources = value;
                PhysicalWorldNaturalResourcesFontWeight = new Windows.UI.Text.FontWeight { Weight = (ushort)(HasRtfContent(value) ? 700 : 400) };
                OnPropertyChanged();
                ShellViewModel.ShowChange();
            }
        }
    }

    public string CurrentPhysicalWorldFlora
    {
        get => HasPhysicalWorlds && CurrentPhysicalWorldIndex < PhysicalWorlds.Count
            ? PhysicalWorlds[CurrentPhysicalWorldIndex].Flora : string.Empty;
        set
        {
            if (HasPhysicalWorlds && CurrentPhysicalWorldIndex < PhysicalWorlds.Count)
            {
                PhysicalWorlds[CurrentPhysicalWorldIndex].Flora = value;
                PhysicalWorldFloraFontWeight = new Windows.UI.Text.FontWeight { Weight = (ushort)(HasRtfContent(value) ? 700 : 400) };
                OnPropertyChanged();
                ShellViewModel.ShowChange();
            }
        }
    }

    public string CurrentPhysicalWorldFauna
    {
        get => HasPhysicalWorlds && CurrentPhysicalWorldIndex < PhysicalWorlds.Count
            ? PhysicalWorlds[CurrentPhysicalWorldIndex].Fauna : string.Empty;
        set
        {
            if (HasPhysicalWorlds && CurrentPhysicalWorldIndex < PhysicalWorlds.Count)
            {
                PhysicalWorlds[CurrentPhysicalWorldIndex].Fauna = value;
                PhysicalWorldFaunaFontWeight = new Windows.UI.Text.FontWeight { Weight = (ushort)(HasRtfContent(value) ? 700 : 400) };
                OnPropertyChanged();
                ShellViewModel.ShowChange();
            }
        }
    }

    public string CurrentPhysicalWorldAstronomy
    {
        get => HasPhysicalWorlds && CurrentPhysicalWorldIndex < PhysicalWorlds.Count
            ? PhysicalWorlds[CurrentPhysicalWorldIndex].Astronomy : string.Empty;
        set
        {
            if (HasPhysicalWorlds && CurrentPhysicalWorldIndex < PhysicalWorlds.Count)
            {
                PhysicalWorlds[CurrentPhysicalWorldIndex].Astronomy = value;
                PhysicalWorldAstronomyFontWeight = new Windows.UI.Text.FontWeight { Weight = (ushort)(HasRtfContent(value) ? 700 : 400) };
                OnPropertyChanged();
                ShellViewModel.ShowChange();
            }
        }
    }

    private void NotifyPhysicalWorldNavigationChanged()
    {
        OnPropertyChanged(nameof(HasPhysicalWorlds));
        OnPropertyChanged(nameof(HasPreviousPhysicalWorld));
        OnPropertyChanged(nameof(HasNextPhysicalWorld));
        OnPropertyChanged(nameof(PhysicalWorldPositionDisplay));
        OnPropertyChanged(nameof(CurrentPhysicalWorldName));
        OnPropertyChanged(nameof(CurrentPhysicalWorldGeography));
        OnPropertyChanged(nameof(CurrentPhysicalWorldClimate));
        OnPropertyChanged(nameof(CurrentPhysicalWorldNaturalResources));
        OnPropertyChanged(nameof(CurrentPhysicalWorldFlora));
        OnPropertyChanged(nameof(CurrentPhysicalWorldFauna));
        OnPropertyChanged(nameof(CurrentPhysicalWorldAstronomy));
        OnPropertyChanged(nameof(RemoveButtonVisibility));
        UpdatePhysicalWorldFontWeights();
    }

    private void PreviousPhysicalWorld()
    {
        if (HasPreviousPhysicalWorld)
        {
            CurrentPhysicalWorldIndex--;
        }
    }

    private void NextPhysicalWorld()
    {
        if (HasNextPhysicalWorld)
        {
            CurrentPhysicalWorldIndex++;
        }
    }

    private void AddPhysicalWorld()
    {
        var entry = new PhysicalWorldEntry { Name = "New World" };
        PhysicalWorlds.Add(entry);
        CurrentPhysicalWorldIndex = PhysicalWorlds.Count - 1; // Navigate to new entry
        NotifyPhysicalWorldNavigationChanged();
        ShellViewModel.ShowChange();
    }

    private void RemoveCurrentPhysicalWorld()
    {
        if (HasPhysicalWorlds && CurrentPhysicalWorldIndex < PhysicalWorlds.Count)
        {
            PhysicalWorlds.RemoveAt(CurrentPhysicalWorldIndex);
            // Adjust index if needed
            if (CurrentPhysicalWorldIndex >= PhysicalWorlds.Count && PhysicalWorlds.Count > 0)
            {
                CurrentPhysicalWorldIndex = PhysicalWorlds.Count - 1;
            }
            NotifyPhysicalWorldNavigationChanged();
            ShellViewModel.ShowChange();
        }
    }

    public void RemovePhysicalWorld(PhysicalWorldEntry entry)
    {
        if (entry != null && PhysicalWorlds.Contains(entry))
        {
            var index = PhysicalWorlds.IndexOf(entry);
            PhysicalWorlds.Remove(entry);
            // Adjust current index if needed
            if (CurrentPhysicalWorldIndex >= PhysicalWorlds.Count && PhysicalWorlds.Count > 0)
            {
                CurrentPhysicalWorldIndex = PhysicalWorlds.Count - 1;
            }
            NotifyPhysicalWorldNavigationChanged();
            ShellViewModel.ShowChange();
        }
    }

    #endregion

    #region Species Navigation

    public RelayCommand AddSpeciesCommand { get; private set; }
    public RelayCommand RemoveCurrentSpeciesCommand { get; private set; }
    public RelayCommand PreviousSpeciesCommand { get; private set; }
    public RelayCommand NextSpeciesCommand { get; private set; }

    private int _currentSpeciesIndex;
    public int CurrentSpeciesIndex
    {
        get => _currentSpeciesIndex;
        set { if (SetProperty(ref _currentSpeciesIndex, value)) NotifySpeciesNavigationChanged(); }
    }

    public bool HasSpecies => Species?.Count > 0;
    public bool HasPreviousSpecies => CurrentSpeciesIndex > 0;
    public bool HasNextSpecies => Species != null && CurrentSpeciesIndex < Species.Count - 1;
    public string SpeciesPositionDisplay => Species == null || Species.Count == 0 ? "0 of 0" : $"{CurrentSpeciesIndex + 1} of {Species.Count}";

    public string CurrentSpeciesName
    {
        get => HasSpecies && CurrentSpeciesIndex < Species.Count ? Species[CurrentSpeciesIndex].Name : string.Empty;
        set { if (HasSpecies && CurrentSpeciesIndex < Species.Count) { Species[CurrentSpeciesIndex].Name = value; OnPropertyChanged(); ShellViewModel.ShowChange(); } }
    }
    public string CurrentSpeciesPhysicalTraits
    {
        get => HasSpecies && CurrentSpeciesIndex < Species.Count ? Species[CurrentSpeciesIndex].PhysicalTraits : string.Empty;
        set { if (HasSpecies && CurrentSpeciesIndex < Species.Count) { Species[CurrentSpeciesIndex].PhysicalTraits = value; SpeciesPhysicalTraitsFontWeight = new Windows.UI.Text.FontWeight { Weight = (ushort)(HasRtfContent(value) ? 700 : 400) }; OnPropertyChanged(); ShellViewModel.ShowChange(); } }
    }
    public string CurrentSpeciesLifespan
    {
        get => HasSpecies && CurrentSpeciesIndex < Species.Count ? Species[CurrentSpeciesIndex].Lifespan : string.Empty;
        set { if (HasSpecies && CurrentSpeciesIndex < Species.Count) { Species[CurrentSpeciesIndex].Lifespan = value; SpeciesLifespanFontWeight = new Windows.UI.Text.FontWeight { Weight = (ushort)(HasRtfContent(value) ? 700 : 400) }; OnPropertyChanged(); ShellViewModel.ShowChange(); } }
    }
    public string CurrentSpeciesOrigins
    {
        get => HasSpecies && CurrentSpeciesIndex < Species.Count ? Species[CurrentSpeciesIndex].Origins : string.Empty;
        set { if (HasSpecies && CurrentSpeciesIndex < Species.Count) { Species[CurrentSpeciesIndex].Origins = value; SpeciesOriginsFontWeight = new Windows.UI.Text.FontWeight { Weight = (ushort)(HasRtfContent(value) ? 700 : 400) }; OnPropertyChanged(); ShellViewModel.ShowChange(); } }
    }
    public string CurrentSpeciesSocialStructure
    {
        get => HasSpecies && CurrentSpeciesIndex < Species.Count ? Species[CurrentSpeciesIndex].SocialStructure : string.Empty;
        set { if (HasSpecies && CurrentSpeciesIndex < Species.Count) { Species[CurrentSpeciesIndex].SocialStructure = value; SpeciesSocialStructureFontWeight = new Windows.UI.Text.FontWeight { Weight = (ushort)(HasRtfContent(value) ? 700 : 400) }; OnPropertyChanged(); ShellViewModel.ShowChange(); } }
    }
    public string CurrentSpeciesDiversity
    {
        get => HasSpecies && CurrentSpeciesIndex < Species.Count ? Species[CurrentSpeciesIndex].Diversity : string.Empty;
        set { if (HasSpecies && CurrentSpeciesIndex < Species.Count) { Species[CurrentSpeciesIndex].Diversity = value; SpeciesDiversityFontWeight = new Windows.UI.Text.FontWeight { Weight = (ushort)(HasRtfContent(value) ? 700 : 400) }; OnPropertyChanged(); ShellViewModel.ShowChange(); } }
    }

    private void NotifySpeciesNavigationChanged()
    {
        OnPropertyChanged(nameof(HasSpecies)); OnPropertyChanged(nameof(HasPreviousSpecies)); OnPropertyChanged(nameof(HasNextSpecies));
        OnPropertyChanged(nameof(SpeciesPositionDisplay)); OnPropertyChanged(nameof(CurrentSpeciesName));
        OnPropertyChanged(nameof(CurrentSpeciesPhysicalTraits)); OnPropertyChanged(nameof(CurrentSpeciesLifespan));
        OnPropertyChanged(nameof(CurrentSpeciesOrigins)); OnPropertyChanged(nameof(CurrentSpeciesSocialStructure)); OnPropertyChanged(nameof(CurrentSpeciesDiversity));
        UpdateSpeciesFontWeights();
    }

    private void PreviousSpecies() { if (HasPreviousSpecies) CurrentSpeciesIndex--; }
    private void NextSpecies() { if (HasNextSpecies) CurrentSpeciesIndex++; }
    private void AddSpecies() { Species.Add(new SpeciesEntry { Name = "New Species" }); CurrentSpeciesIndex = Species.Count - 1; NotifySpeciesNavigationChanged(); ShellViewModel.ShowChange(); }
    private void RemoveCurrentSpecies() { if (HasSpecies && CurrentSpeciesIndex < Species.Count) { Species.RemoveAt(CurrentSpeciesIndex); if (CurrentSpeciesIndex >= Species.Count && Species.Count > 0) CurrentSpeciesIndex = Species.Count - 1; NotifySpeciesNavigationChanged(); ShellViewModel.ShowChange(); } }
    public void RemoveSpecies(SpeciesEntry entry) { if (entry != null && Species.Contains(entry)) { Species.Remove(entry); if (CurrentSpeciesIndex >= Species.Count && Species.Count > 0) CurrentSpeciesIndex = Species.Count - 1; NotifySpeciesNavigationChanged(); ShellViewModel.ShowChange(); } }

    #endregion

    #region Culture Navigation

    public RelayCommand AddCultureCommand { get; private set; }
    public RelayCommand RemoveCurrentCultureCommand { get; private set; }
    public RelayCommand PreviousCultureCommand { get; private set; }
    public RelayCommand NextCultureCommand { get; private set; }

    private int _currentCultureIndex;
    public int CurrentCultureIndex
    {
        get => _currentCultureIndex;
        set { if (SetProperty(ref _currentCultureIndex, value)) NotifyCultureNavigationChanged(); }
    }

    public bool HasCultures => Cultures?.Count > 0;
    public bool HasPreviousCulture => CurrentCultureIndex > 0;
    public bool HasNextCulture => Cultures != null && CurrentCultureIndex < Cultures.Count - 1;
    public string CulturePositionDisplay => Cultures == null || Cultures.Count == 0 ? "0 of 0" : $"{CurrentCultureIndex + 1} of {Cultures.Count}";

    public string CurrentCultureName
    {
        get => HasCultures && CurrentCultureIndex < Cultures.Count ? Cultures[CurrentCultureIndex].Name : string.Empty;
        set { if (HasCultures && CurrentCultureIndex < Cultures.Count) { Cultures[CurrentCultureIndex].Name = value; OnPropertyChanged(); ShellViewModel.ShowChange(); } }
    }
    public string CurrentCultureValues
    {
        get => HasCultures && CurrentCultureIndex < Cultures.Count ? Cultures[CurrentCultureIndex].Values : string.Empty;
        set
        {
            if (HasCultures && CurrentCultureIndex < Cultures.Count)
            {
                Cultures[CurrentCultureIndex].Values = value;
                CultureValuesFontWeight = new Windows.UI.Text.FontWeight { Weight = (ushort)(HasRtfContent(value) ? 700 : 400) };
                OnPropertyChanged();
                ShellViewModel.ShowChange();
            }
        }
    }
    public string CurrentCultureCustoms
    {
        get => HasCultures && CurrentCultureIndex < Cultures.Count ? Cultures[CurrentCultureIndex].Customs : string.Empty;
        set
        {
            if (HasCultures && CurrentCultureIndex < Cultures.Count)
            {
                Cultures[CurrentCultureIndex].Customs = value;
                CultureCustomsFontWeight = new Windows.UI.Text.FontWeight { Weight = (ushort)(HasRtfContent(value) ? 700 : 400) };
                OnPropertyChanged();
                ShellViewModel.ShowChange();
            }
        }
    }
    public string CurrentCultureTaboos
    {
        get => HasCultures && CurrentCultureIndex < Cultures.Count ? Cultures[CurrentCultureIndex].Taboos : string.Empty;
        set
        {
            if (HasCultures && CurrentCultureIndex < Cultures.Count)
            {
                Cultures[CurrentCultureIndex].Taboos = value;
                CultureTaboosFontWeight = new Windows.UI.Text.FontWeight { Weight = (ushort)(HasRtfContent(value) ? 700 : 400) };
                OnPropertyChanged();
                ShellViewModel.ShowChange();
            }
        }
    }
    public string CurrentCultureArt
    {
        get => HasCultures && CurrentCultureIndex < Cultures.Count ? Cultures[CurrentCultureIndex].Art : string.Empty;
        set
        {
            if (HasCultures && CurrentCultureIndex < Cultures.Count)
            {
                Cultures[CurrentCultureIndex].Art = value;
                CultureArtFontWeight = new Windows.UI.Text.FontWeight { Weight = (ushort)(HasRtfContent(value) ? 700 : 400) };
                OnPropertyChanged();
                ShellViewModel.ShowChange();
            }
        }
    }
    public string CurrentCultureDailyLife
    {
        get => HasCultures && CurrentCultureIndex < Cultures.Count ? Cultures[CurrentCultureIndex].DailyLife : string.Empty;
        set
        {
            if (HasCultures && CurrentCultureIndex < Cultures.Count)
            {
                Cultures[CurrentCultureIndex].DailyLife = value;
                CultureDailyLifeFontWeight = new Windows.UI.Text.FontWeight { Weight = (ushort)(HasRtfContent(value) ? 700 : 400) };
                OnPropertyChanged();
                ShellViewModel.ShowChange();
            }
        }
    }
    public string CurrentCultureEntertainment
    {
        get => HasCultures && CurrentCultureIndex < Cultures.Count ? Cultures[CurrentCultureIndex].Entertainment : string.Empty;
        set
        {
            if (HasCultures && CurrentCultureIndex < Cultures.Count)
            {
                Cultures[CurrentCultureIndex].Entertainment = value;
                CultureEntertainmentFontWeight = new Windows.UI.Text.FontWeight { Weight = (ushort)(HasRtfContent(value) ? 700 : 400) };
                OnPropertyChanged();
                ShellViewModel.ShowChange();
            }
        }
    }

    private void NotifyCultureNavigationChanged()
    {
        OnPropertyChanged(nameof(HasCultures)); OnPropertyChanged(nameof(HasPreviousCulture)); OnPropertyChanged(nameof(HasNextCulture));
        OnPropertyChanged(nameof(CulturePositionDisplay)); OnPropertyChanged(nameof(CurrentCultureName));
        OnPropertyChanged(nameof(CurrentCultureValues)); OnPropertyChanged(nameof(CurrentCultureCustoms)); OnPropertyChanged(nameof(CurrentCultureTaboos));
        OnPropertyChanged(nameof(CurrentCultureArt)); OnPropertyChanged(nameof(CurrentCultureDailyLife)); OnPropertyChanged(nameof(CurrentCultureEntertainment));
        UpdateCultureFontWeights();
    }

    private void PreviousCulture() { if (HasPreviousCulture) CurrentCultureIndex--; }
    private void NextCulture() { if (HasNextCulture) CurrentCultureIndex++; }
    private void AddCulture() { Cultures.Add(new CultureEntry { Name = "New Culture" }); CurrentCultureIndex = Cultures.Count - 1; NotifyCultureNavigationChanged(); ShellViewModel.ShowChange(); }
    private void RemoveCurrentCulture() { if (HasCultures && CurrentCultureIndex < Cultures.Count) { Cultures.RemoveAt(CurrentCultureIndex); if (CurrentCultureIndex >= Cultures.Count && Cultures.Count > 0) CurrentCultureIndex = Cultures.Count - 1; NotifyCultureNavigationChanged(); ShellViewModel.ShowChange(); } }
    public void RemoveCulture(CultureEntry entry) { if (entry != null && Cultures.Contains(entry)) { Cultures.Remove(entry); if (CurrentCultureIndex >= Cultures.Count && Cultures.Count > 0) CurrentCultureIndex = Cultures.Count - 1; NotifyCultureNavigationChanged(); ShellViewModel.ShowChange(); } }

    #endregion

    #region Content Indicators

    /// <summary>
    /// Helper to check if RTF content has meaningful text.
    /// RichEditBoxExtended normalizes empty content to empty string.
    /// </summary>
    private static bool HasRtfContent(string rtfText) => !string.IsNullOrEmpty(rtfText);

    // Physical World FontWeights
    private Windows.UI.Text.FontWeight _physicalWorldGeographyFontWeight;
    public Windows.UI.Text.FontWeight PhysicalWorldGeographyFontWeight
    {
        get => _physicalWorldGeographyFontWeight;
        set => SetProperty(ref _physicalWorldGeographyFontWeight, value);
    }

    private Windows.UI.Text.FontWeight _physicalWorldClimateFontWeight;
    public Windows.UI.Text.FontWeight PhysicalWorldClimateFontWeight
    {
        get => _physicalWorldClimateFontWeight;
        set => SetProperty(ref _physicalWorldClimateFontWeight, value);
    }

    private Windows.UI.Text.FontWeight _physicalWorldNaturalResourcesFontWeight;
    public Windows.UI.Text.FontWeight PhysicalWorldNaturalResourcesFontWeight
    {
        get => _physicalWorldNaturalResourcesFontWeight;
        set => SetProperty(ref _physicalWorldNaturalResourcesFontWeight, value);
    }

    private Windows.UI.Text.FontWeight _physicalWorldFloraFontWeight;
    public Windows.UI.Text.FontWeight PhysicalWorldFloraFontWeight
    {
        get => _physicalWorldFloraFontWeight;
        set => SetProperty(ref _physicalWorldFloraFontWeight, value);
    }

    private Windows.UI.Text.FontWeight _physicalWorldFaunaFontWeight;
    public Windows.UI.Text.FontWeight PhysicalWorldFaunaFontWeight
    {
        get => _physicalWorldFaunaFontWeight;
        set => SetProperty(ref _physicalWorldFaunaFontWeight, value);
    }

    private Windows.UI.Text.FontWeight _physicalWorldAstronomyFontWeight;
    public Windows.UI.Text.FontWeight PhysicalWorldAstronomyFontWeight
    {
        get => _physicalWorldAstronomyFontWeight;
        set => SetProperty(ref _physicalWorldAstronomyFontWeight, value);
    }

    private void UpdatePhysicalWorldFontWeights()
    {
        var bold = new Windows.UI.Text.FontWeight { Weight = 700 };
        var normal = new Windows.UI.Text.FontWeight { Weight = 400 };

        PhysicalWorldGeographyFontWeight = HasRtfContent(CurrentPhysicalWorldGeography) ? bold : normal;
        PhysicalWorldClimateFontWeight = HasRtfContent(CurrentPhysicalWorldClimate) ? bold : normal;
        PhysicalWorldNaturalResourcesFontWeight = HasRtfContent(CurrentPhysicalWorldNaturalResources) ? bold : normal;
        PhysicalWorldFloraFontWeight = HasRtfContent(CurrentPhysicalWorldFlora) ? bold : normal;
        PhysicalWorldFaunaFontWeight = HasRtfContent(CurrentPhysicalWorldFauna) ? bold : normal;
        PhysicalWorldAstronomyFontWeight = HasRtfContent(CurrentPhysicalWorldAstronomy) ? bold : normal;
    }

    // Species FontWeights
    private Windows.UI.Text.FontWeight _speciesPhysicalTraitsFontWeight;
    public Windows.UI.Text.FontWeight SpeciesPhysicalTraitsFontWeight
    {
        get => _speciesPhysicalTraitsFontWeight;
        set => SetProperty(ref _speciesPhysicalTraitsFontWeight, value);
    }

    private Windows.UI.Text.FontWeight _speciesLifespanFontWeight;
    public Windows.UI.Text.FontWeight SpeciesLifespanFontWeight
    {
        get => _speciesLifespanFontWeight;
        set => SetProperty(ref _speciesLifespanFontWeight, value);
    }

    private Windows.UI.Text.FontWeight _speciesOriginsFontWeight;
    public Windows.UI.Text.FontWeight SpeciesOriginsFontWeight
    {
        get => _speciesOriginsFontWeight;
        set => SetProperty(ref _speciesOriginsFontWeight, value);
    }

    private Windows.UI.Text.FontWeight _speciesSocialStructureFontWeight;
    public Windows.UI.Text.FontWeight SpeciesSocialStructureFontWeight
    {
        get => _speciesSocialStructureFontWeight;
        set => SetProperty(ref _speciesSocialStructureFontWeight, value);
    }

    private Windows.UI.Text.FontWeight _speciesDiversityFontWeight;
    public Windows.UI.Text.FontWeight SpeciesDiversityFontWeight
    {
        get => _speciesDiversityFontWeight;
        set => SetProperty(ref _speciesDiversityFontWeight, value);
    }

    private void UpdateSpeciesFontWeights()
    {
        var bold = new Windows.UI.Text.FontWeight { Weight = 700 };
        var normal = new Windows.UI.Text.FontWeight { Weight = 400 };

        SpeciesPhysicalTraitsFontWeight = HasRtfContent(CurrentSpeciesPhysicalTraits) ? bold : normal;
        SpeciesLifespanFontWeight = HasRtfContent(CurrentSpeciesLifespan) ? bold : normal;
        SpeciesOriginsFontWeight = HasRtfContent(CurrentSpeciesOrigins) ? bold : normal;
        SpeciesSocialStructureFontWeight = HasRtfContent(CurrentSpeciesSocialStructure) ? bold : normal;
        SpeciesDiversityFontWeight = HasRtfContent(CurrentSpeciesDiversity) ? bold : normal;
    }

    // Culture FontWeights
    private Windows.UI.Text.FontWeight _cultureValuesFontWeight;
    public Windows.UI.Text.FontWeight CultureValuesFontWeight
    {
        get => _cultureValuesFontWeight;
        set => SetProperty(ref _cultureValuesFontWeight, value);
    }

    private Windows.UI.Text.FontWeight _cultureCustomsFontWeight;
    public Windows.UI.Text.FontWeight CultureCustomsFontWeight
    {
        get => _cultureCustomsFontWeight;
        set => SetProperty(ref _cultureCustomsFontWeight, value);
    }

    private Windows.UI.Text.FontWeight _cultureTaboosFontWeight;
    public Windows.UI.Text.FontWeight CultureTaboosFontWeight
    {
        get => _cultureTaboosFontWeight;
        set => SetProperty(ref _cultureTaboosFontWeight, value);
    }

    private Windows.UI.Text.FontWeight _cultureArtFontWeight;
    public Windows.UI.Text.FontWeight CultureArtFontWeight
    {
        get => _cultureArtFontWeight;
        set => SetProperty(ref _cultureArtFontWeight, value);
    }

    private Windows.UI.Text.FontWeight _cultureDailyLifeFontWeight;
    public Windows.UI.Text.FontWeight CultureDailyLifeFontWeight
    {
        get => _cultureDailyLifeFontWeight;
        set => SetProperty(ref _cultureDailyLifeFontWeight, value);
    }

    private Windows.UI.Text.FontWeight _cultureEntertainmentFontWeight;
    public Windows.UI.Text.FontWeight CultureEntertainmentFontWeight
    {
        get => _cultureEntertainmentFontWeight;
        set => SetProperty(ref _cultureEntertainmentFontWeight, value);
    }

    private void UpdateCultureFontWeights()
    {
        var bold = new Windows.UI.Text.FontWeight { Weight = 700 };
        var normal = new Windows.UI.Text.FontWeight { Weight = 400 };

        CultureValuesFontWeight = HasRtfContent(CurrentCultureValues) ? bold : normal;
        CultureCustomsFontWeight = HasRtfContent(CurrentCultureCustoms) ? bold : normal;
        CultureTaboosFontWeight = HasRtfContent(CurrentCultureTaboos) ? bold : normal;
        CultureArtFontWeight = HasRtfContent(CurrentCultureArt) ? bold : normal;
        CultureDailyLifeFontWeight = HasRtfContent(CurrentCultureDailyLife) ? bold : normal;
        CultureEntertainmentFontWeight = HasRtfContent(CurrentCultureEntertainment) ? bold : normal;
    }

    // Government FontWeights
    private Windows.UI.Text.FontWeight _governmentTypeFontWeight;
    public Windows.UI.Text.FontWeight GovernmentTypeFontWeight
    {
        get => _governmentTypeFontWeight;
        set => SetProperty(ref _governmentTypeFontWeight, value);
    }

    private Windows.UI.Text.FontWeight _governmentPowerStructuresFontWeight;
    public Windows.UI.Text.FontWeight GovernmentPowerStructuresFontWeight
    {
        get => _governmentPowerStructuresFontWeight;
        set => SetProperty(ref _governmentPowerStructuresFontWeight, value);
    }

    private Windows.UI.Text.FontWeight _governmentLawsFontWeight;
    public Windows.UI.Text.FontWeight GovernmentLawsFontWeight
    {
        get => _governmentLawsFontWeight;
        set => SetProperty(ref _governmentLawsFontWeight, value);
    }

    private Windows.UI.Text.FontWeight _governmentClassStructureFontWeight;
    public Windows.UI.Text.FontWeight GovernmentClassStructureFontWeight
    {
        get => _governmentClassStructureFontWeight;
        set => SetProperty(ref _governmentClassStructureFontWeight, value);
    }

    private Windows.UI.Text.FontWeight _governmentForeignRelationsFontWeight;
    public Windows.UI.Text.FontWeight GovernmentForeignRelationsFontWeight
    {
        get => _governmentForeignRelationsFontWeight;
        set => SetProperty(ref _governmentForeignRelationsFontWeight, value);
    }

    private void UpdateGovernmentFontWeights()
    {
        var bold = new Windows.UI.Text.FontWeight { Weight = 700 };
        var normal = new Windows.UI.Text.FontWeight { Weight = 400 };

        GovernmentTypeFontWeight = HasRtfContent(CurrentGovernmentType) ? bold : normal;
        GovernmentPowerStructuresFontWeight = HasRtfContent(CurrentGovernmentPowerStructures) ? bold : normal;
        GovernmentLawsFontWeight = HasRtfContent(CurrentGovernmentLaws) ? bold : normal;
        GovernmentClassStructureFontWeight = HasRtfContent(CurrentGovernmentClassStructure) ? bold : normal;
        GovernmentForeignRelationsFontWeight = HasRtfContent(CurrentGovernmentForeignRelations) ? bold : normal;
    }

    // Religion FontWeights
    private Windows.UI.Text.FontWeight _religionDeitiesFontWeight;
    public Windows.UI.Text.FontWeight ReligionDeitiesFontWeight
    {
        get => _religionDeitiesFontWeight;
        set => SetProperty(ref _religionDeitiesFontWeight, value);
    }

    private Windows.UI.Text.FontWeight _religionBeliefsFontWeight;
    public Windows.UI.Text.FontWeight ReligionBeliefsFontWeight
    {
        get => _religionBeliefsFontWeight;
        set => SetProperty(ref _religionBeliefsFontWeight, value);
    }

    private Windows.UI.Text.FontWeight _religionPracticesFontWeight;
    public Windows.UI.Text.FontWeight ReligionPracticesFontWeight
    {
        get => _religionPracticesFontWeight;
        set => SetProperty(ref _religionPracticesFontWeight, value);
    }

    private Windows.UI.Text.FontWeight _religionOrganizationsFontWeight;
    public Windows.UI.Text.FontWeight ReligionOrganizationsFontWeight
    {
        get => _religionOrganizationsFontWeight;
        set => SetProperty(ref _religionOrganizationsFontWeight, value);
    }

    private Windows.UI.Text.FontWeight _religionCreationMythsFontWeight;
    public Windows.UI.Text.FontWeight ReligionCreationMythsFontWeight
    {
        get => _religionCreationMythsFontWeight;
        set => SetProperty(ref _religionCreationMythsFontWeight, value);
    }

    private void UpdateReligionFontWeights()
    {
        var bold = new Windows.UI.Text.FontWeight { Weight = 700 };
        var normal = new Windows.UI.Text.FontWeight { Weight = 400 };

        ReligionDeitiesFontWeight = HasRtfContent(CurrentReligionDeities) ? bold : normal;
        ReligionBeliefsFontWeight = HasRtfContent(CurrentReligionBeliefs) ? bold : normal;
        ReligionPracticesFontWeight = HasRtfContent(CurrentReligionPractices) ? bold : normal;
        ReligionOrganizationsFontWeight = HasRtfContent(CurrentReligionOrganizations) ? bold : normal;
        ReligionCreationMythsFontWeight = HasRtfContent(CurrentReligionCreationMyths) ? bold : normal;
    }

    // History FontWeights
    private Windows.UI.Text.FontWeight _foundingEventsFontWeight;
    public Windows.UI.Text.FontWeight FoundingEventsFontWeight
    {
        get => _foundingEventsFontWeight;
        set => SetProperty(ref _foundingEventsFontWeight, value);
    }

    private Windows.UI.Text.FontWeight _majorConflictsFontWeight;
    public Windows.UI.Text.FontWeight MajorConflictsFontWeight
    {
        get => _majorConflictsFontWeight;
        set => SetProperty(ref _majorConflictsFontWeight, value);
    }

    private Windows.UI.Text.FontWeight _erasFontWeight;
    public Windows.UI.Text.FontWeight ErasFontWeight
    {
        get => _erasFontWeight;
        set => SetProperty(ref _erasFontWeight, value);
    }

    private Windows.UI.Text.FontWeight _technologicalShiftsFontWeight;
    public Windows.UI.Text.FontWeight TechnologicalShiftsFontWeight
    {
        get => _technologicalShiftsFontWeight;
        set => SetProperty(ref _technologicalShiftsFontWeight, value);
    }

    private Windows.UI.Text.FontWeight _lostKnowledgeFontWeight;
    public Windows.UI.Text.FontWeight LostKnowledgeFontWeight
    {
        get => _lostKnowledgeFontWeight;
        set => SetProperty(ref _lostKnowledgeFontWeight, value);
    }

    private void UpdateHistoryFontWeights()
    {
        var bold = new Windows.UI.Text.FontWeight { Weight = 700 };
        var normal = new Windows.UI.Text.FontWeight { Weight = 400 };

        FoundingEventsFontWeight = HasRtfContent(FoundingEvents) ? bold : normal;
        MajorConflictsFontWeight = HasRtfContent(MajorConflicts) ? bold : normal;
        ErasFontWeight = HasRtfContent(Eras) ? bold : normal;
        TechnologicalShiftsFontWeight = HasRtfContent(TechnologicalShifts) ? bold : normal;
        LostKnowledgeFontWeight = HasRtfContent(LostKnowledge) ? bold : normal;
    }

    // Economy FontWeights
    private Windows.UI.Text.FontWeight _economicSystemFontWeight;
    public Windows.UI.Text.FontWeight EconomicSystemFontWeight
    {
        get => _economicSystemFontWeight;
        set => SetProperty(ref _economicSystemFontWeight, value);
    }

    private Windows.UI.Text.FontWeight _currencyFontWeight;
    public Windows.UI.Text.FontWeight CurrencyFontWeight
    {
        get => _currencyFontWeight;
        set => SetProperty(ref _currencyFontWeight, value);
    }

    private Windows.UI.Text.FontWeight _tradeRoutesFontWeight;
    public Windows.UI.Text.FontWeight TradeRoutesFontWeight
    {
        get => _tradeRoutesFontWeight;
        set => SetProperty(ref _tradeRoutesFontWeight, value);
    }

    private Windows.UI.Text.FontWeight _professionsFontWeight;
    public Windows.UI.Text.FontWeight ProfessionsFontWeight
    {
        get => _professionsFontWeight;
        set => SetProperty(ref _professionsFontWeight, value);
    }

    private Windows.UI.Text.FontWeight _wealthDistributionFontWeight;
    public Windows.UI.Text.FontWeight WealthDistributionFontWeight
    {
        get => _wealthDistributionFontWeight;
        set => SetProperty(ref _wealthDistributionFontWeight, value);
    }

    private void UpdateEconomyFontWeights()
    {
        var bold = new Windows.UI.Text.FontWeight { Weight = 700 };
        var normal = new Windows.UI.Text.FontWeight { Weight = 400 };

        EconomicSystemFontWeight = HasRtfContent(EconomicSystem) ? bold : normal;
        CurrencyFontWeight = HasRtfContent(Currency) ? bold : normal;
        TradeRoutesFontWeight = HasRtfContent(TradeRoutes) ? bold : normal;
        ProfessionsFontWeight = HasRtfContent(Professions) ? bold : normal;
        WealthDistributionFontWeight = HasRtfContent(WealthDistribution) ? bold : normal;
    }

    // Magic/Tech FontWeights
    private Windows.UI.Text.FontWeight _sourceFontWeight;
    public Windows.UI.Text.FontWeight SourceFontWeight
    {
        get => _sourceFontWeight;
        set => SetProperty(ref _sourceFontWeight, value);
    }

    private Windows.UI.Text.FontWeight _rulesFontWeight;
    public Windows.UI.Text.FontWeight RulesFontWeight
    {
        get => _rulesFontWeight;
        set => SetProperty(ref _rulesFontWeight, value);
    }

    private Windows.UI.Text.FontWeight _limitationsFontWeight;
    public Windows.UI.Text.FontWeight LimitationsFontWeight
    {
        get => _limitationsFontWeight;
        set => SetProperty(ref _limitationsFontWeight, value);
    }

    private Windows.UI.Text.FontWeight _costFontWeight;
    public Windows.UI.Text.FontWeight CostFontWeight
    {
        get => _costFontWeight;
        set => SetProperty(ref _costFontWeight, value);
    }

    private Windows.UI.Text.FontWeight _practitionersFontWeight;
    public Windows.UI.Text.FontWeight PractitionersFontWeight
    {
        get => _practitionersFontWeight;
        set => SetProperty(ref _practitionersFontWeight, value);
    }

    private Windows.UI.Text.FontWeight _socialImpactFontWeight;
    public Windows.UI.Text.FontWeight SocialImpactFontWeight
    {
        get => _socialImpactFontWeight;
        set => SetProperty(ref _socialImpactFontWeight, value);
    }

    private void UpdateMagicTechFontWeights()
    {
        var bold = new Windows.UI.Text.FontWeight { Weight = 700 };
        var normal = new Windows.UI.Text.FontWeight { Weight = 400 };

        SourceFontWeight = HasRtfContent(Source) ? bold : normal;
        RulesFontWeight = HasRtfContent(Rules) ? bold : normal;
        LimitationsFontWeight = HasRtfContent(Limitations) ? bold : normal;
        CostFontWeight = HasRtfContent(Cost) ? bold : normal;
        PractitionersFontWeight = HasRtfContent(Practitioners) ? bold : normal;
        SocialImpactFontWeight = HasRtfContent(SocialImpact) ? bold : normal;
    }

    #endregion

    #region Government Navigation

    public RelayCommand AddGovernmentCommand { get; private set; }
    public RelayCommand RemoveCurrentGovernmentCommand { get; private set; }
    public RelayCommand PreviousGovernmentCommand { get; private set; }
    public RelayCommand NextGovernmentCommand { get; private set; }

    private int _currentGovernmentIndex;
    public int CurrentGovernmentIndex
    {
        get => _currentGovernmentIndex;
        set { if (SetProperty(ref _currentGovernmentIndex, value)) NotifyGovernmentNavigationChanged(); }
    }

    public bool HasGovernments => Governments?.Count > 0;
    public bool HasPreviousGovernment => CurrentGovernmentIndex > 0;
    public bool HasNextGovernment => Governments != null && CurrentGovernmentIndex < Governments.Count - 1;
    public string GovernmentPositionDisplay => Governments == null || Governments.Count == 0 ? "0 of 0" : $"{CurrentGovernmentIndex + 1} of {Governments.Count}";

    public string CurrentGovernmentName
    {
        get => HasGovernments && CurrentGovernmentIndex < Governments.Count ? Governments[CurrentGovernmentIndex].Name : string.Empty;
        set { if (HasGovernments && CurrentGovernmentIndex < Governments.Count) { Governments[CurrentGovernmentIndex].Name = value; OnPropertyChanged(); ShellViewModel.ShowChange(); } }
    }
    public string CurrentGovernmentType
    {
        get => HasGovernments && CurrentGovernmentIndex < Governments.Count ? Governments[CurrentGovernmentIndex].Type : string.Empty;
        set { if (HasGovernments && CurrentGovernmentIndex < Governments.Count) { Governments[CurrentGovernmentIndex].Type = value; GovernmentTypeFontWeight = new Windows.UI.Text.FontWeight { Weight = (ushort)(HasRtfContent(value) ? 700 : 400) }; OnPropertyChanged(); ShellViewModel.ShowChange(); } }
    }
    public string CurrentGovernmentPowerStructures
    {
        get => HasGovernments && CurrentGovernmentIndex < Governments.Count ? Governments[CurrentGovernmentIndex].PowerStructures : string.Empty;
        set { if (HasGovernments && CurrentGovernmentIndex < Governments.Count) { Governments[CurrentGovernmentIndex].PowerStructures = value; GovernmentPowerStructuresFontWeight = new Windows.UI.Text.FontWeight { Weight = (ushort)(HasRtfContent(value) ? 700 : 400) }; OnPropertyChanged(); ShellViewModel.ShowChange(); } }
    }
    public string CurrentGovernmentLaws
    {
        get => HasGovernments && CurrentGovernmentIndex < Governments.Count ? Governments[CurrentGovernmentIndex].Laws : string.Empty;
        set { if (HasGovernments && CurrentGovernmentIndex < Governments.Count) { Governments[CurrentGovernmentIndex].Laws = value; GovernmentLawsFontWeight = new Windows.UI.Text.FontWeight { Weight = (ushort)(HasRtfContent(value) ? 700 : 400) }; OnPropertyChanged(); ShellViewModel.ShowChange(); } }
    }
    public string CurrentGovernmentClassStructure
    {
        get => HasGovernments && CurrentGovernmentIndex < Governments.Count ? Governments[CurrentGovernmentIndex].ClassStructure : string.Empty;
        set { if (HasGovernments && CurrentGovernmentIndex < Governments.Count) { Governments[CurrentGovernmentIndex].ClassStructure = value; GovernmentClassStructureFontWeight = new Windows.UI.Text.FontWeight { Weight = (ushort)(HasRtfContent(value) ? 700 : 400) }; OnPropertyChanged(); ShellViewModel.ShowChange(); } }
    }
    public string CurrentGovernmentForeignRelations
    {
        get => HasGovernments && CurrentGovernmentIndex < Governments.Count ? Governments[CurrentGovernmentIndex].ForeignRelations : string.Empty;
        set { if (HasGovernments && CurrentGovernmentIndex < Governments.Count) { Governments[CurrentGovernmentIndex].ForeignRelations = value; GovernmentForeignRelationsFontWeight = new Windows.UI.Text.FontWeight { Weight = (ushort)(HasRtfContent(value) ? 700 : 400) }; OnPropertyChanged(); ShellViewModel.ShowChange(); } }
    }

    private void NotifyGovernmentNavigationChanged()
    {
        OnPropertyChanged(nameof(HasGovernments)); OnPropertyChanged(nameof(HasPreviousGovernment)); OnPropertyChanged(nameof(HasNextGovernment));
        OnPropertyChanged(nameof(GovernmentPositionDisplay)); OnPropertyChanged(nameof(CurrentGovernmentName));
        OnPropertyChanged(nameof(CurrentGovernmentType)); OnPropertyChanged(nameof(CurrentGovernmentPowerStructures));
        OnPropertyChanged(nameof(CurrentGovernmentLaws)); OnPropertyChanged(nameof(CurrentGovernmentClassStructure)); OnPropertyChanged(nameof(CurrentGovernmentForeignRelations));
        UpdateGovernmentFontWeights();
    }

    private void PreviousGovernment() { if (HasPreviousGovernment) CurrentGovernmentIndex--; }
    private void NextGovernment() { if (HasNextGovernment) CurrentGovernmentIndex++; }
    private void AddGovernment() { Governments.Add(new GovernmentEntry { Name = "New Government" }); CurrentGovernmentIndex = Governments.Count - 1; NotifyGovernmentNavigationChanged(); ShellViewModel.ShowChange(); }
    private void RemoveCurrentGovernment() { if (HasGovernments && CurrentGovernmentIndex < Governments.Count) { Governments.RemoveAt(CurrentGovernmentIndex); if (CurrentGovernmentIndex >= Governments.Count && Governments.Count > 0) CurrentGovernmentIndex = Governments.Count - 1; NotifyGovernmentNavigationChanged(); ShellViewModel.ShowChange(); } }
    public void RemoveGovernment(GovernmentEntry entry) { if (entry != null && Governments.Contains(entry)) { Governments.Remove(entry); if (CurrentGovernmentIndex >= Governments.Count && Governments.Count > 0) CurrentGovernmentIndex = Governments.Count - 1; NotifyGovernmentNavigationChanged(); ShellViewModel.ShowChange(); } }

    #endregion

    #region Religion Navigation

    public RelayCommand AddReligionCommand { get; private set; }
    public RelayCommand RemoveCurrentReligionCommand { get; private set; }
    public RelayCommand PreviousReligionCommand { get; private set; }
    public RelayCommand NextReligionCommand { get; private set; }

    private int _currentReligionIndex;
    public int CurrentReligionIndex
    {
        get => _currentReligionIndex;
        set { if (SetProperty(ref _currentReligionIndex, value)) NotifyReligionNavigationChanged(); }
    }

    public bool HasReligions => Religions?.Count > 0;
    public bool HasPreviousReligion => CurrentReligionIndex > 0;
    public bool HasNextReligion => Religions != null && CurrentReligionIndex < Religions.Count - 1;
    public string ReligionPositionDisplay => Religions == null || Religions.Count == 0 ? "0 of 0" : $"{CurrentReligionIndex + 1} of {Religions.Count}";

    public string CurrentReligionName
    {
        get => HasReligions && CurrentReligionIndex < Religions.Count ? Religions[CurrentReligionIndex].Name : string.Empty;
        set { if (HasReligions && CurrentReligionIndex < Religions.Count) { Religions[CurrentReligionIndex].Name = value; OnPropertyChanged(); ShellViewModel.ShowChange(); } }
    }
    public string CurrentReligionDeities
    {
        get => HasReligions && CurrentReligionIndex < Religions.Count ? Religions[CurrentReligionIndex].Deities : string.Empty;
        set { if (HasReligions && CurrentReligionIndex < Religions.Count) { Religions[CurrentReligionIndex].Deities = value; ReligionDeitiesFontWeight = new Windows.UI.Text.FontWeight { Weight = (ushort)(HasRtfContent(value) ? 700 : 400) }; OnPropertyChanged(); ShellViewModel.ShowChange(); } }
    }
    public string CurrentReligionBeliefs
    {
        get => HasReligions && CurrentReligionIndex < Religions.Count ? Religions[CurrentReligionIndex].Beliefs : string.Empty;
        set { if (HasReligions && CurrentReligionIndex < Religions.Count) { Religions[CurrentReligionIndex].Beliefs = value; ReligionBeliefsFontWeight = new Windows.UI.Text.FontWeight { Weight = (ushort)(HasRtfContent(value) ? 700 : 400) }; OnPropertyChanged(); ShellViewModel.ShowChange(); } }
    }
    public string CurrentReligionPractices
    {
        get => HasReligions && CurrentReligionIndex < Religions.Count ? Religions[CurrentReligionIndex].Practices : string.Empty;
        set { if (HasReligions && CurrentReligionIndex < Religions.Count) { Religions[CurrentReligionIndex].Practices = value; ReligionPracticesFontWeight = new Windows.UI.Text.FontWeight { Weight = (ushort)(HasRtfContent(value) ? 700 : 400) }; OnPropertyChanged(); ShellViewModel.ShowChange(); } }
    }
    public string CurrentReligionOrganizations
    {
        get => HasReligions && CurrentReligionIndex < Religions.Count ? Religions[CurrentReligionIndex].Organizations : string.Empty;
        set { if (HasReligions && CurrentReligionIndex < Religions.Count) { Religions[CurrentReligionIndex].Organizations = value; ReligionOrganizationsFontWeight = new Windows.UI.Text.FontWeight { Weight = (ushort)(HasRtfContent(value) ? 700 : 400) }; OnPropertyChanged(); ShellViewModel.ShowChange(); } }
    }
    public string CurrentReligionCreationMyths
    {
        get => HasReligions && CurrentReligionIndex < Religions.Count ? Religions[CurrentReligionIndex].CreationMyths : string.Empty;
        set { if (HasReligions && CurrentReligionIndex < Religions.Count) { Religions[CurrentReligionIndex].CreationMyths = value; ReligionCreationMythsFontWeight = new Windows.UI.Text.FontWeight { Weight = (ushort)(HasRtfContent(value) ? 700 : 400) }; OnPropertyChanged(); ShellViewModel.ShowChange(); } }
    }

    private void NotifyReligionNavigationChanged()
    {
        OnPropertyChanged(nameof(HasReligions)); OnPropertyChanged(nameof(HasPreviousReligion)); OnPropertyChanged(nameof(HasNextReligion));
        OnPropertyChanged(nameof(ReligionPositionDisplay)); OnPropertyChanged(nameof(CurrentReligionName));
        OnPropertyChanged(nameof(CurrentReligionDeities)); OnPropertyChanged(nameof(CurrentReligionBeliefs));
        OnPropertyChanged(nameof(CurrentReligionPractices)); OnPropertyChanged(nameof(CurrentReligionOrganizations)); OnPropertyChanged(nameof(CurrentReligionCreationMyths));
        UpdateReligionFontWeights();
    }

    private void PreviousReligion() { if (HasPreviousReligion) CurrentReligionIndex--; }
    private void NextReligion() { if (HasNextReligion) CurrentReligionIndex++; }
    private void AddReligion() { Religions.Add(new ReligionEntry { Name = "New Religion" }); CurrentReligionIndex = Religions.Count - 1; NotifyReligionNavigationChanged(); ShellViewModel.ShowChange(); }
    private void RemoveCurrentReligion() { if (HasReligions && CurrentReligionIndex < Religions.Count) { Religions.RemoveAt(CurrentReligionIndex); if (CurrentReligionIndex >= Religions.Count && Religions.Count > 0) CurrentReligionIndex = Religions.Count - 1; NotifyReligionNavigationChanged(); ShellViewModel.ShowChange(); } }
    public void RemoveReligion(ReligionEntry entry) { if (entry != null && Religions.Contains(entry)) { Religions.Remove(entry); if (CurrentReligionIndex >= Religions.Count && Religions.Count > 0) CurrentReligionIndex = Religions.Count - 1; NotifyReligionNavigationChanged(); ShellViewModel.ShowChange(); } }

    #endregion

    #endregion

    #region World Type Description Properties

    private string _worldTypeDescription;
    /// <summary>
    /// Full description of the selected World Type.
    /// </summary>
    public string WorldTypeDescription
    {
        get => _worldTypeDescription;
        private set => SetProperty(ref _worldTypeDescription, value);
    }

    private string _worldTypeExamples;
    /// <summary>
    /// Example works for the selected World Type.
    /// </summary>
    public string WorldTypeExamples
    {
        get => _worldTypeExamples;
        private set => SetProperty(ref _worldTypeExamples, value);
    }

    #endregion

    #region Tab Selection and Context-Sensitive Buttons

    private int _selectedTabIndex;
    /// <summary>
    /// The currently selected tab index. Used for context-sensitive Add/Remove buttons.
    /// Tabs: 0=Structure, 1=Physical World, 2=Species, 3=Cultures, 4=Governments, 5=Religions, 6=History, 7=Economy, 8=Magic/Tech, 9=AI Parameters
    /// </summary>
    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set
        {
            if (SetProperty(ref _selectedTabIndex, value))
            {
                OnPropertyChanged(nameof(AddButtonLabel));
                OnPropertyChanged(nameof(AddButtonVisibility));
                OnPropertyChanged(nameof(RemoveButtonVisibility));
            }
        }
    }

    /// <summary>
    /// Label for the Add button based on selected tab.
    /// </summary>
    public string AddButtonLabel => SelectedTabIndex switch
    {
        1 => "+ Add World",
        2 => "+ Add Species",
        3 => "+ Add Culture",
        4 => "+ Add Government",
        5 => "+ Add Religion",
        _ => "+ Add"
    };

    /// <summary>
    /// Add button is only visible for list tabs (1-5).
    /// </summary>
    public Visibility AddButtonVisibility =>
        SelectedTabIndex >= 1 && SelectedTabIndex <= 5 ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Remove button is only visible when there are entries to remove in the current list tab.
    /// </summary>
    public Visibility RemoveButtonVisibility
    {
        get
        {
            var hasEntries = SelectedTabIndex switch
            {
                1 => PhysicalWorlds?.Count > 0,
                2 => Species?.Count > 0,
                3 => Cultures?.Count > 0,
                4 => Governments?.Count > 0,
                5 => Religions?.Count > 0,
                _ => false
            };
            return hasEntries ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    /// <summary>
    /// Unified Add command that adds to the appropriate list based on selected tab.
    /// </summary>
    public RelayCommand AddEntryCommand { get; private set; }

    private void AddEntry()
    {
        switch (SelectedTabIndex)
        {
            case 1: AddPhysicalWorld(); break;
            case 2: AddSpecies(); break;
            case 3: AddCulture(); break;
            case 4: AddGovernment(); break;
            case 5: AddReligion(); break;
        }
        OnPropertyChanged(nameof(RemoveButtonVisibility));
    }

    #endregion

    #region List Tab Properties

    public ObservableCollection<PhysicalWorldEntry> PhysicalWorlds { get; private set; }
    public ObservableCollection<SpeciesEntry> Species { get; private set; }
    public ObservableCollection<CultureEntry> Cultures { get; private set; }
    public ObservableCollection<GovernmentEntry> Governments { get; private set; }
    public ObservableCollection<ReligionEntry> Religions { get; private set; }

    #endregion

    #region History Tab Properties

    private string _foundingEvents;
    public string FoundingEvents
    {
        get => _foundingEvents;
        set
        {
            if (SetProperty(ref _foundingEvents, value))
            {
                FoundingEventsFontWeight = new Windows.UI.Text.FontWeight { Weight = (ushort)(HasRtfContent(value) ? 700 : 400) };
            }
        }
    }

    private string _majorConflicts;
    public string MajorConflicts
    {
        get => _majorConflicts;
        set
        {
            if (SetProperty(ref _majorConflicts, value))
            {
                MajorConflictsFontWeight = new Windows.UI.Text.FontWeight { Weight = (ushort)(HasRtfContent(value) ? 700 : 400) };
            }
        }
    }

    private string _eras;
    public string Eras
    {
        get => _eras;
        set
        {
            if (SetProperty(ref _eras, value))
            {
                ErasFontWeight = new Windows.UI.Text.FontWeight { Weight = (ushort)(HasRtfContent(value) ? 700 : 400) };
            }
        }
    }

    private string _technologicalShifts;
    public string TechnologicalShifts
    {
        get => _technologicalShifts;
        set
        {
            if (SetProperty(ref _technologicalShifts, value))
            {
                TechnologicalShiftsFontWeight = new Windows.UI.Text.FontWeight { Weight = (ushort)(HasRtfContent(value) ? 700 : 400) };
            }
        }
    }

    private string _lostKnowledge;
    public string LostKnowledge
    {
        get => _lostKnowledge;
        set
        {
            if (SetProperty(ref _lostKnowledge, value))
            {
                LostKnowledgeFontWeight = new Windows.UI.Text.FontWeight { Weight = (ushort)(HasRtfContent(value) ? 700 : 400) };
            }
        }
    }

    #endregion

    #region Economy Tab Properties

    private string _economicSystem;
    public string EconomicSystem
    {
        get => _economicSystem;
        set
        {
            if (SetProperty(ref _economicSystem, value))
            {
                EconomicSystemFontWeight = new Windows.UI.Text.FontWeight { Weight = (ushort)(HasRtfContent(value) ? 700 : 400) };
            }
        }
    }

    private string _currency;
    public string Currency
    {
        get => _currency;
        set
        {
            if (SetProperty(ref _currency, value))
            {
                CurrencyFontWeight = new Windows.UI.Text.FontWeight { Weight = (ushort)(HasRtfContent(value) ? 700 : 400) };
            }
        }
    }

    private string _tradeRoutes;
    public string TradeRoutes
    {
        get => _tradeRoutes;
        set
        {
            if (SetProperty(ref _tradeRoutes, value))
            {
                TradeRoutesFontWeight = new Windows.UI.Text.FontWeight { Weight = (ushort)(HasRtfContent(value) ? 700 : 400) };
            }
        }
    }

    private string _professions;
    public string Professions
    {
        get => _professions;
        set
        {
            if (SetProperty(ref _professions, value))
            {
                ProfessionsFontWeight = new Windows.UI.Text.FontWeight { Weight = (ushort)(HasRtfContent(value) ? 700 : 400) };
            }
        }
    }

    private string _wealthDistribution;
    public string WealthDistribution
    {
        get => _wealthDistribution;
        set
        {
            if (SetProperty(ref _wealthDistribution, value))
            {
                WealthDistributionFontWeight = new Windows.UI.Text.FontWeight { Weight = (ushort)(HasRtfContent(value) ? 700 : 400) };
            }
        }
    }

    #endregion

    #region Magic/Technology Tab Properties

    private string _systemType;
    public string SystemType
    {
        get => _systemType;
        set => SetProperty(ref _systemType, value);
    }

    private string _source;
    public string Source
    {
        get => _source;
        set
        {
            if (SetProperty(ref _source, value))
            {
                SourceFontWeight = new Windows.UI.Text.FontWeight { Weight = (ushort)(HasRtfContent(value) ? 700 : 400) };
            }
        }
    }

    private string _rules;
    public string Rules
    {
        get => _rules;
        set
        {
            if (SetProperty(ref _rules, value))
            {
                RulesFontWeight = new Windows.UI.Text.FontWeight { Weight = (ushort)(HasRtfContent(value) ? 700 : 400) };
            }
        }
    }

    private string _limitations;
    public string Limitations
    {
        get => _limitations;
        set
        {
            if (SetProperty(ref _limitations, value))
            {
                LimitationsFontWeight = new Windows.UI.Text.FontWeight { Weight = (ushort)(HasRtfContent(value) ? 700 : 400) };
            }
        }
    }

    private string _cost;
    public string Cost
    {
        get => _cost;
        set
        {
            if (SetProperty(ref _cost, value))
            {
                CostFontWeight = new Windows.UI.Text.FontWeight { Weight = (ushort)(HasRtfContent(value) ? 700 : 400) };
            }
        }
    }

    private string _practitioners;
    public string Practitioners
    {
        get => _practitioners;
        set
        {
            if (SetProperty(ref _practitioners, value))
            {
                PractitionersFontWeight = new Windows.UI.Text.FontWeight { Weight = (ushort)(HasRtfContent(value) ? 700 : 400) };
            }
        }
    }

    private string _socialImpact;
    public string SocialImpact
    {
        get => _socialImpact;
        set
        {
            if (SetProperty(ref _socialImpact, value))
            {
                SocialImpactFontWeight = new Windows.UI.Text.FontWeight { Weight = (ushort)(HasRtfContent(value) ? 700 : 400) };
            }
        }
    }

    #endregion

    #region ComboBox ItemsSource Collections

    /// <summary>
    /// The 8 World Type gestalts for the dropdown.
    /// </summary>
    public ObservableCollection<string> WorldTypeList { get; private set; }

    // 6 Axis lists for advanced options
    public ObservableCollection<string> OntologyList { get; private set; }
    public ObservableCollection<string> WorldRelationList { get; private set; }
    public ObservableCollection<string> RuleTransparencyList { get; private set; }
    public ObservableCollection<string> ScaleOfDifferenceList { get; private set; }
    public ObservableCollection<string> AgencySourceList { get; private set; }
    public ObservableCollection<string> ToneLogicList { get; private set; }
    public ObservableCollection<string> SystemTypeList { get; private set; }

    #endregion

    #region INavigable Implementation

    public void Activate(object parameter)
    {
        var param = parameter as StoryWorldModel;
        _logger.Log(LogLevel.Info, $"StoryWorldViewModel.Activate: parameter={param?.Name} (Uuid={param?.Uuid})");
        _changeable = false;
        _changed = false;
        Model = (StoryWorldModel)parameter;
        _logger.Log(LogLevel.Info, $"StoryWorldViewModel.Activate: Model set to {Model?.Name} (Uuid={Model?.Uuid})");
        LoadModel();
    }

    public void Deactivate(object parameter)
    {
        _logger.Log(LogLevel.Info, $"StoryWorldViewModel.Deactivate: Saving to Model={Model?.Name} (Uuid={Model?.Uuid})");
        SaveModel();
    }

    #endregion

    #region ISaveable / IReloadable Implementation

    public void SaveModel()
    {
        try
        {
            // Note: Don't save Name - it's derived for display, node keeps "Story World"

            // Structure tab
            Model.WorldType = WorldType;
            Model.Ontology = Ontology;
            Model.WorldRelation = WorldRelation;
            Model.RuleTransparency = RuleTransparency;
            Model.ScaleOfDifference = ScaleOfDifference;
            Model.AgencySource = AgencySource;
            Model.ToneLogic = ToneLogic;

            // Lists - copy from ObservableCollection to List
            Model.PhysicalWorlds = new List<PhysicalWorldEntry>(PhysicalWorlds);
            Model.Species = new List<SpeciesEntry>(Species);
            Model.Cultures = new List<CultureEntry>(Cultures);
            Model.Governments = new List<GovernmentEntry>(Governments);
            Model.Religions = new List<ReligionEntry>(Religions);

            // History tab
            Model.FoundingEvents = FoundingEvents;
            Model.MajorConflicts = MajorConflicts;
            Model.Eras = Eras;
            Model.TechnologicalShifts = TechnologicalShifts;
            Model.LostKnowledge = LostKnowledge;

            // Economy tab
            Model.EconomicSystem = EconomicSystem;
            Model.Currency = Currency;
            Model.TradeRoutes = TradeRoutes;
            Model.Professions = Professions;
            Model.WealthDistribution = WealthDistribution;

            // Magic/Technology tab
            Model.SystemType = SystemType;
            Model.Source = Source;
            Model.Rules = Rules;
            Model.Limitations = Limitations;
            Model.Cost = Cost;
            Model.Practitioners = Practitioners;
            Model.SocialImpact = SocialImpact;
        }
        catch (Exception ex)
        {
            _logger.LogException(LogLevel.Error, ex, $"Failed to save StoryWorld model - {ex.Message}");
        }
    }

    public void ReloadFromModel()
    {
        if (Model != null)
        {
            LoadModel();
        }
    }

    private void LoadModel()
    {
        _changeable = false;
        _changed = false;

        // StoryElement properties
        Uuid = Model.Uuid;
        // Derive display name from story name (first node in ExplorerView is OverviewModel)
        var storyModel = _appState.CurrentDocument?.Model;
        var storyName = storyModel?.ExplorerView.Count > 0
            ? storyModel.ExplorerView[0].Name?.Trim() ?? string.Empty
            : string.Empty;
        Name = string.IsNullOrEmpty(storyName)
            ? "Story World"
            : storyName + " Story World";

        // Structure tab
        WorldType = Model.WorldType;
        Ontology = Model.Ontology;
        WorldRelation = Model.WorldRelation;
        RuleTransparency = Model.RuleTransparency;
        ScaleOfDifference = Model.ScaleOfDifference;
        AgencySource = Model.AgencySource;
        ToneLogic = Model.ToneLogic;

        // Lists - copy from Model to ObservableCollection
        PhysicalWorlds.Clear();
        foreach (var entry in Model.PhysicalWorlds)
            PhysicalWorlds.Add(entry);
        _currentPhysicalWorldIndex = 0; // Reset to first entry
        NotifyPhysicalWorldNavigationChanged();

        Species.Clear();
        foreach (var entry in Model.Species)
            Species.Add(entry);
        _currentSpeciesIndex = 0;
        NotifySpeciesNavigationChanged();

        Cultures.Clear();
        foreach (var entry in Model.Cultures)
            Cultures.Add(entry);
        _currentCultureIndex = 0;
        NotifyCultureNavigationChanged();

        Governments.Clear();
        foreach (var entry in Model.Governments)
            Governments.Add(entry);
        _currentGovernmentIndex = 0;
        NotifyGovernmentNavigationChanged();

        Religions.Clear();
        foreach (var entry in Model.Religions)
            Religions.Add(entry);
        _currentReligionIndex = 0;
        NotifyReligionNavigationChanged();

        // History tab
        FoundingEvents = Model.FoundingEvents;
        MajorConflicts = Model.MajorConflicts;
        Eras = Model.Eras;
        TechnologicalShifts = Model.TechnologicalShifts;
        LostKnowledge = Model.LostKnowledge;
        UpdateHistoryFontWeights();

        // Economy tab
        EconomicSystem = Model.EconomicSystem;
        Currency = Model.Currency;
        TradeRoutes = Model.TradeRoutes;
        Professions = Model.Professions;
        WealthDistribution = Model.WealthDistribution;
        UpdateEconomyFontWeights();

        // Magic/Technology tab
        SystemType = Model.SystemType;
        Source = Model.Source;
        Rules = Model.Rules;
        Limitations = Model.Limitations;
        Cost = Model.Cost;
        Practitioners = Model.Practitioners;
        SocialImpact = Model.SocialImpact;
        UpdateMagicTechFontWeights();

        _changeable = true;
    }

    #endregion

    #region Property Change Handling

    private void OnPropertyChanged(object sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(Model))
            return;

        if (_changeable)
        {
            if (!_changed)
            {
                _logger.Log(LogLevel.Info, $"StoryWorldViewModel.OnPropertyChanged: {args.PropertyName} changed");
            }
            _changed = true;
            ShellViewModel.ShowChange();
        }
    }

    #endregion

    #region World Type Mapping

    /// <summary>
    /// Updates the description and examples based on the selected World Type.
    /// </summary>
    private void UpdateWorldTypeDescriptions()
    {
        var (description, examples) = GetWorldTypeInfo(WorldType);
        WorldTypeDescription = description;
        WorldTypeExamples = examples;
    }

    /// <summary>
    /// Auto-populates axis values based on the selected World Type.
    /// Uses the gestalt-to-axis mapping from design documents.
    /// </summary>
    private void AutoPopulateAxisValues()
    {
        var axes = GetAxisValuesForWorldType(WorldType);
        if (axes == null) return;

        Ontology = axes.Ontology;
        WorldRelation = axes.WorldRelation;
        RuleTransparency = axes.RuleTransparency;
        ScaleOfDifference = axes.ScaleOfDifference;
        AgencySource = axes.AgencySource;
        ToneLogic = axes.ToneLogic;
    }

    /// <summary>
    /// Returns description and examples for a World Type.
    /// </summary>
    private static (string Description, string Examples) GetWorldTypeInfo(string worldType)
    {
        return worldType switch
        {
            "Consensus Reality" => (
                "The world operates exactly as expected - no magic, no hidden layers, no alternate physics. " +
                "\"Consensus\" refers to a specific group or subculture whose reality you're depicting. " +
                "Every consensus reality story still requires worldbuilding the norms, rules, and insider knowledge of that particular slice of life.",
                "87th Precinct • Harry Bosch • Rabbit series • Grisham novels • Big Little Lies"),

            "Enchanted Reality" => (
                "Our world, but reality is porous. The impossible happens and is accepted rather than analyzed. " +
                "Magic or the supernatural exists but isn't systematized - it simply is. " +
                "This is the realm of magical realism and slipstream fiction.",
                "One Hundred Years of Solitude • Like Water for Chocolate • Beloved • Pan's Labyrinth"),

            "Hidden World" => (
                "Our world, but with concealed magical or supernatural layers beneath or alongside normal reality. " +
                "The \"mundane\" world operates normally, but a secret realm exists that most people don't know about. " +
                "Discovery of this hidden layer is often dangerous or comes at a cost.",
                "Harry Potter • Dresden Files • American Gods • The Matrix • Men in Black • Percy Jackson"),

            "Divergent World" => (
                "Our world, but history or conditions diverged at some point. " +
                "The rules of reality remain rational and logical, but society, technology, or events developed differently. " +
                "This includes alternate history, steampunk, cyberpunk, and near-future speculation.",
                "The Man in the High Castle • 11/22/63 • Neuromancer • The Handmaid's Tale"),

            "Constructed World" => (
                "A fully invented reality with its own geography, history, peoples, and rules. " +
                "The world doesn't derive from Earth at all - it's built from scratch. " +
                "This is the realm of epic fantasy and secondary-world science fiction. The author defines everything.",
                "A Song of Ice and Fire • Discworld • Dune • Star Wars • The Stormlight Archive"),

            "Mythic World" => (
                "A world where narrative meaning matters more than physical causality. " +
                "Fate, prophecy, and archetypes drive events. Things happen because they're meaningful, not because of cause and effect. " +
                "Gods and destiny are real forces.",
                "The Lord of the Rings • Earthsea • The Chronicles of Narnia • Circe • The Once and Future King"),

            "Estranged World" => (
                "A world that feels fundamentally alien or wrong. Rules may exist, but they resist human intuition. " +
                "The familiar becomes strange. " +
                "This is the realm of cosmic horror, New Weird, and hard SF that emphasizes how truly alien the universe can be.",
                "Solaris • Annihilation • Perdido Street Station • Blindsight • 2001: A Space Odyssey"),

            "Broken World" => (
                "A world where civilization, environment, or social order has collapsed or been corrupted. " +
                "Survival replaces progress. Resources are scarce, institutions have failed, and the focus is on enduring rather than building. " +
                "Includes post-apocalyptic and dystopian settings.",
                "The Road • Mad Max • 1984 • The Walking Dead • Station Eleven • A Canticle for Leibowitz"),

            _ => ("Select a World Type to see its description.", "")
        };
    }

    /// <summary>
    /// Returns axis values for a World Type based on the gestalt-to-axis mapping.
    /// </summary>
    private static AxisValues GetAxisValuesForWorldType(string worldType)
    {
        return worldType switch
        {
            "Consensus Reality" => new AxisValues
            {
                Ontology = "Mundane",
                WorldRelation = "Primary World",
                RuleTransparency = "Explicit Rules",
                ScaleOfDifference = "Cosmetic",
                AgencySource = "Human-Centric",
                ToneLogic = "Rational"
            },
            "Enchanted Reality" => new AxisValues
            {
                Ontology = "Supernatural",
                WorldRelation = "Primary World",
                RuleTransparency = "Implicit Rules",
                ScaleOfDifference = "Cosmetic",
                AgencySource = "Systemic Forces",
                ToneLogic = "Symbolic"
            },
            "Hidden World" => new AxisValues
            {
                Ontology = "Supernatural",
                WorldRelation = "Layered",
                RuleTransparency = "Explicit Rules",
                ScaleOfDifference = "Structural",
                AgencySource = "Nonhuman Intelligences",
                ToneLogic = "Rational"
            },
            "Divergent World" => new AxisValues
            {
                Ontology = "Scientific Speculative",
                WorldRelation = "Divergent Earth",
                RuleTransparency = "Explicit Rules",
                ScaleOfDifference = "Structural",
                AgencySource = "Human-Centric",
                ToneLogic = "Rational"
            },
            "Constructed World" => new AxisValues
            {
                Ontology = "Hybrid",
                WorldRelation = "Secondary World",
                RuleTransparency = "Explicit Rules",
                ScaleOfDifference = "Cosmological",
                AgencySource = "Human-Centric",  // Variable in spec, default to Human-Centric
                ToneLogic = "Rational"           // Variable in spec, default to Rational
            },
            "Mythic World" => new AxisValues
            {
                Ontology = "Symbolic",
                WorldRelation = "Secondary World",
                RuleTransparency = "Symbolic Rules",
                ScaleOfDifference = "Cosmological",
                AgencySource = "Fate / Providence",
                ToneLogic = "Mythic"
            },
            "Estranged World" => new AxisValues
            {
                Ontology = "Scientific Speculative",
                WorldRelation = "Secondary World",  // Variable in spec
                RuleTransparency = "Explicit Rules",
                ScaleOfDifference = "Cosmological",
                AgencySource = "Systemic Forces",
                ToneLogic = "Dark / Entropic"
            },
            "Broken World" => new AxisValues
            {
                Ontology = "Scientific Speculative",
                WorldRelation = "Divergent Earth",
                RuleTransparency = "Explicit Rules",
                ScaleOfDifference = "Structural",
                AgencySource = "Human-Centric",
                ToneLogic = "Dark / Entropic"
            },
            _ => null
        };
    }

    /// <summary>
    /// Helper class to hold axis values for mapping.
    /// </summary>
    private class AxisValues
    {
        public string Ontology { get; init; }
        public string WorldRelation { get; init; }
        public string RuleTransparency { get; init; }
        public string ScaleOfDifference { get; init; }
        public string AgencySource { get; init; }
        public string ToneLogic { get; init; }
    }

    #endregion

    #region Constructor

    public StoryWorldViewModel(ILogService logger, ListData listData, AppState appState, CollaboratorService collaboratorService)
    {
        _logger = logger;
        _appState = appState;
        _collaboratorService = collaboratorService;
        PropertyChanged += OnPropertyChanged;

        // Initialize commands
        AddPhysicalWorldCommand = new RelayCommand(AddPhysicalWorld);
        RemoveCurrentPhysicalWorldCommand = new RelayCommand(RemoveCurrentPhysicalWorld);
        PreviousPhysicalWorldCommand = new RelayCommand(PreviousPhysicalWorld);
        NextPhysicalWorldCommand = new RelayCommand(NextPhysicalWorld);
        AddSpeciesCommand = new RelayCommand(AddSpecies);
        RemoveCurrentSpeciesCommand = new RelayCommand(RemoveCurrentSpecies);
        PreviousSpeciesCommand = new RelayCommand(PreviousSpecies);
        NextSpeciesCommand = new RelayCommand(NextSpecies);
        AddCultureCommand = new RelayCommand(AddCulture);
        RemoveCurrentCultureCommand = new RelayCommand(RemoveCurrentCulture);
        PreviousCultureCommand = new RelayCommand(PreviousCulture);
        NextCultureCommand = new RelayCommand(NextCulture);
        AddGovernmentCommand = new RelayCommand(AddGovernment);
        RemoveCurrentGovernmentCommand = new RelayCommand(RemoveCurrentGovernment);
        PreviousGovernmentCommand = new RelayCommand(PreviousGovernment);
        NextGovernmentCommand = new RelayCommand(NextGovernment);
        AddReligionCommand = new RelayCommand(AddReligion);
        RemoveCurrentReligionCommand = new RelayCommand(RemoveCurrentReligion);
        PreviousReligionCommand = new RelayCommand(PreviousReligion);
        NextReligionCommand = new RelayCommand(NextReligion);
        AddEntryCommand = new RelayCommand(AddEntry);

        // Initialize description properties
        WorldTypeDescription = "Select a World Type to see its description.";
        WorldTypeExamples = string.Empty;

        // Initialize string properties
        Name = string.Empty;
        WorldType = string.Empty;
        Ontology = string.Empty;
        WorldRelation = string.Empty;
        RuleTransparency = string.Empty;
        ScaleOfDifference = string.Empty;
        AgencySource = string.Empty;
        ToneLogic = string.Empty;
        FoundingEvents = string.Empty;
        MajorConflicts = string.Empty;
        Eras = string.Empty;
        TechnologicalShifts = string.Empty;
        LostKnowledge = string.Empty;
        EconomicSystem = string.Empty;
        Currency = string.Empty;
        TradeRoutes = string.Empty;
        Professions = string.Empty;
        WealthDistribution = string.Empty;
        SystemType = string.Empty;
        Source = string.Empty;
        Rules = string.Empty;
        Limitations = string.Empty;
        Cost = string.Empty;
        Practitioners = string.Empty;
        SocialImpact = string.Empty;

        // Initialize list tab collections
        PhysicalWorlds = new ObservableCollection<PhysicalWorldEntry>();
        Species = new ObservableCollection<SpeciesEntry>();
        Cultures = new ObservableCollection<CultureEntry>();
        Governments = new ObservableCollection<GovernmentEntry>();
        Religions = new ObservableCollection<ReligionEntry>();

        // Initialize all FontWeights to normal
        var normalWeight = new Windows.UI.Text.FontWeight { Weight = 400 };

        // Physical World
        _physicalWorldGeographyFontWeight = normalWeight;
        _physicalWorldClimateFontWeight = normalWeight;
        _physicalWorldNaturalResourcesFontWeight = normalWeight;
        _physicalWorldFloraFontWeight = normalWeight;
        _physicalWorldFaunaFontWeight = normalWeight;
        _physicalWorldAstronomyFontWeight = normalWeight;

        // Species
        _speciesPhysicalTraitsFontWeight = normalWeight;
        _speciesLifespanFontWeight = normalWeight;
        _speciesOriginsFontWeight = normalWeight;
        _speciesSocialStructureFontWeight = normalWeight;
        _speciesDiversityFontWeight = normalWeight;

        // Culture
        _cultureValuesFontWeight = normalWeight;
        _cultureCustomsFontWeight = normalWeight;
        _cultureTaboosFontWeight = normalWeight;
        _cultureArtFontWeight = normalWeight;
        _cultureDailyLifeFontWeight = normalWeight;
        _cultureEntertainmentFontWeight = normalWeight;

        // Government
        _governmentTypeFontWeight = normalWeight;
        _governmentPowerStructuresFontWeight = normalWeight;
        _governmentLawsFontWeight = normalWeight;
        _governmentClassStructureFontWeight = normalWeight;
        _governmentForeignRelationsFontWeight = normalWeight;

        // Religion
        _religionDeitiesFontWeight = normalWeight;
        _religionBeliefsFontWeight = normalWeight;
        _religionPracticesFontWeight = normalWeight;
        _religionOrganizationsFontWeight = normalWeight;
        _religionCreationMythsFontWeight = normalWeight;

        // History
        _foundingEventsFontWeight = normalWeight;
        _majorConflictsFontWeight = normalWeight;
        _erasFontWeight = normalWeight;
        _technologicalShiftsFontWeight = normalWeight;
        _lostKnowledgeFontWeight = normalWeight;

        // Economy
        _economicSystemFontWeight = normalWeight;
        _currencyFontWeight = normalWeight;
        _tradeRoutesFontWeight = normalWeight;
        _professionsFontWeight = normalWeight;
        _wealthDistributionFontWeight = normalWeight;

        // Magic/Tech
        _sourceFontWeight = normalWeight;
        _rulesFontWeight = normalWeight;
        _limitationsFontWeight = normalWeight;
        _costFontWeight = normalWeight;
        _practitionersFontWeight = normalWeight;
        _socialImpactFontWeight = normalWeight;

        // Load ComboBox source collections from ListData
        var _lists = listData.ListControlSource;
        WorldTypeList = _lists["WorldType"];
        OntologyList = _lists["Ontology"];
        WorldRelationList = _lists["WorldRelation"];
        RuleTransparencyList = _lists["RuleTransparency"];
        ScaleOfDifferenceList = _lists["ScaleOfDifference"];
        AgencySourceList = _lists["AgencySource"];
        ToneLogicList = _lists["ToneLogic"];
        SystemTypeList = _lists["SystemType"];
    }

    #endregion
}
