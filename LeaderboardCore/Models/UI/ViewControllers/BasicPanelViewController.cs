using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.ViewControllers;
using HMUI;
using IPA.Utilities;
using System.ComponentModel;
using System.Reflection;
using UnityEngine;

namespace LeaderboardCore.Models.UI.ViewControllers
{
    /// <summary>
    /// Abstract class for a basic panel view. Must provide a logo and custom view for the right side of the panel.
    /// </summary>
    public abstract class BasicPanelViewController : BSMLResourceViewController, INotifyPropertyChanged
    {
        /// <summary>The embedded resource defining the panel layout.</summary>
        public override string ResourceName { get => "LeaderboardCore.Models.UI.Views.basic-panel.bsml"; }

        /// <summary>The BSML source for this panel.</summary>
        public override string Content
        {
            get => BeatSaberMarkupLanguage.Utilities.GetResourceContent(Assembly.GetAssembly(typeof(BasicPanelViewController)), ResourceName);
        }

        #region events

        /// <summary>Raises the inherited notification used by BSML bindings.</summary>
        protected virtual void OnPropertyChanged(PropertyChangedEventArgs eventArgs)
        {
            NotifyPropertyChanged(eventArgs.PropertyName);
        }
        #endregion

        #region UI Components
        /// <summary>The panel background container.</summary>
        [UIComponent("outer")]
        protected Backgroundable outer;

        /// <summary>The image separating the logo from custom content.</summary>
        [UIComponent("separator")]
        protected ImageView separator;

        /// <summary>The interactive logo image.</summary>
        [UIComponent("clickable-logo")]
        protected ClickableImage clickableLogo;

        /// <summary>The noninteractive logo image.</summary>
        [UIComponent("plain-logo")]
        protected ImageView plainLogo;
        #endregion

        #region Logo
        /// <summary>
        /// Whether or not the logo is clickable.
        /// </summary>
        protected virtual bool IsLogoClickable { get => false; }

        /// <summary>
        /// The namespace location of the logo image.
        /// </summary>
        protected abstract string LogoSource { get; }

        /// <summary>
        /// What hover hint to display when hovering over the logo.
        /// </summary>
        protected virtual string LogoHoverHint { get => ""; }

        /// <summary>
        /// Called when the logo is clicked (if <see cref="IsLogoClickable"/> is true).
        /// </summary>
        protected virtual void OnLogoClicked() {}

        /// <summary>Whether the interactive logo is visible.</summary>
        [UIValue("is-logo-clickable")]
        protected bool isLogoClickable { get => IsLogoClickable; }

        /// <summary>Whether the noninteractive logo is visible.</summary>
        [UIValue("is-logo-plain")]
        protected bool isLogoNonClickable { get => !isLogoClickable; }

        /// <summary>The logo resource exposed to BSML.</summary>
        [UIValue("logo-source")]
        protected string logoSource { get => LogoSource; }

        /// <summary>The tooltip exposed by the logo.</summary>
        [UIValue("logo-hoverhint")]
        protected string logoHoverHint { get => LogoHoverHint; }

        /// <summary>Forwards the logo click to the implementation.</summary>
        [UIAction("clicked-logo")]
        protected void ClickedLogo() { OnLogoClicked(); }
        #endregion

        #region Loading
        /// <summary>Backing state indicating whether panel content has loaded.</summary>
        protected bool __isLoaded = false;

        /// <summary>
        /// Set this to indicate whether or not the content is currently loaded. When false, a loading icon will display on the panel.
        /// </summary>
        [UIValue("is-loaded")]
        protected bool isLoaded
        {
            get => __isLoaded;
            set
            {
                __isLoaded = value;
                NotifyPropertyChanged(nameof(isLoaded));
                NotifyPropertyChanged(nameof(isLoading));
            }
        }

        /// <summary>Whether to display the loading indicator.</summary>
        [UIValue("is-loading")]
        protected bool isLoading { get => !isLoaded; }
        #endregion

        #region RightSide
        private bool _parsedCustomBSML = false;

        /// <summary>
        /// The raw bsml content to display on the right side of the panel.
        /// </summary>
        protected abstract string customBSML { get; }

        /// <summary>
        /// The object that handles the UIValues, UIActions, and UIEvents for the given <see cref="customBSML"/>.
        /// </summary>
        protected abstract object customHost { get; }

        /// <summary>The container receiving the implementation's custom BSML.</summary>
        [UIObject("panel-custom")]
        protected GameObject panelCustom;

        #endregion

        #region Background
        private Color _backgroundColor = Color.black;

        /// <summary>
        /// Set this to change the background color of the panel.
        /// </summary>
        protected Color backgroundColor
        {
            get => _backgroundColor;
            set
            {
                _backgroundColor = value;
                if (_background)
                {
                    _background.color = _backgroundColor;
                }
            }
        }
        private ImageView _background;
        #endregion

        /// <summary>Applies panel styling and parses custom content after BSML creates the view.</summary>
        [UIAction("#post-parse")]
        public virtual void Parsed()
        {
            _background = outer.Background as ImageView;

            // show some actual color
            _background.material = BeatSaberMarkupLanguage.Utilities.ImageResources.NoGlowMat;
            _background.color = _backgroundColor;

            // setup gradient
            _background.color0 = Color.white;
            _background.color1 = new Color(1, 1, 1, 0);
            _background.SetField<ImageView, bool>("_gradient", true);

            // skew everything
            _background.SetField<ImageView, float>("_skew", 0.18f);
            separator.SetField<ImageView, float>("_skew", 0.18f);
            clickableLogo.SetField<ImageView, float>("_skew", 0.18f);
            plainLogo.SetField<ImageView, float>("_skew", 0.18f);

            if (!_parsedCustomBSML)
            {
                _parsedCustomBSML = true;
                BSMLParser.Instance.Parse(customBSML, panelCustom, customHost);
            }
        }
    }
}
