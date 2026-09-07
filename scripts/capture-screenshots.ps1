# scripts/capture-screenshots.ps1
# Automates generating high-resolution screenshots for Flow.Launcher.Plugin.Snippets README

param(
    [string]$OutputDir = "$PSScriptRoot/../docs/images"
)

$ErrorActionPreference = "Stop"

# Ensure STA mode for WPF
if ([System.Threading.Thread]::CurrentThread.GetApartmentState() -ne [System.Threading.ApartmentState]::STA) {
    Write-Host "Restarting script in STA mode..."
    & pwsh -STA -NoProfile -File $PSCommandPath @args
    exit $LASTEXITCODE
}

# Ensure output directory exists
$resolvedOutputDir = [System.IO.Path]::GetFullPath($OutputDir)
if (-not (Test-Path $resolvedOutputDir)) {
    New-Item -ItemType Directory -Force -Path $resolvedOutputDir | Out-Null
}

Write-Host "Output directory: $resolvedOutputDir"

# Locate plugin DLL
$dllPath = [System.IO.Path]::GetFullPath("$PSScriptRoot/../Flow.Launcher.Plugin.Snippets/bin/Debug/Flow.Launcher.Plugin.Snippets.dll")
if (-not (Test-Path $dllPath)) {
    Write-Host "Building project first..."
    dotnet build "$PSScriptRoot/../Flow.Launcher.Plugin.Snippets/Flow.Launcher.Plugin.Snippets.csproj" -c Debug
}

# Run in an STA thread because WPF requires STA
$code = {
    param($dllPath, $outputDir, $scriptRoot)

    Add-Type -AssemblyName PresentationFramework
    Add-Type -AssemblyName PresentationCore
    Add-Type -AssemblyName WindowsBase
    Add-Type -AssemblyName System.Xaml

    [System.Reflection.Assembly]::LoadFrom($dllPath) | Out-Null

    # Helper function to save a visual to PNG
    function Save-VisualToPng($visual, $width, $height, $filePath) {
        $visual.Measure([System.Windows.Size]::new($width, $height))
        $visual.Arrange([System.Windows.Rect]::new(0, 0, $width, $height))
        $visual.UpdateLayout()

        $rtb = [System.Windows.Media.Imaging.RenderTargetBitmap]::new($width, $height, 96, 96, [System.Windows.Media.PixelFormats]::Pbgra32)
        $rtb.Render($visual)

        $encoder = [System.Windows.Media.Imaging.PngBitmapEncoder]::new()
        $encoder.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($rtb))

        $stream = [System.IO.File]::Create($filePath)
        try {
            $encoder.Save($stream)
        } finally {
            $stream.Dispose()
        }
        Write-Host "Saved: $filePath"
    }

    # Ensure Application and theme resources
    if ($null -eq [System.Windows.Application]::Current) {
        $app = [System.Windows.Application]::new()
    } else {
        $app = [System.Windows.Application]::Current
    }

    # Flow Launcher Win11 style colors
    $app.Resources["Color00B"] = [System.Windows.Media.SolidColorBrush]::new([System.Windows.Media.Color]::FromArgb(255, 255, 255, 255))
    $app.Resources["Color01B"] = [System.Windows.Media.SolidColorBrush]::new([System.Windows.Media.Color]::FromArgb(255, 245, 245, 247))
    $app.Resources["Color02B"] = [System.Windows.Media.SolidColorBrush]::new([System.Windows.Media.Color]::FromArgb(255, 235, 237, 240))
    $app.Resources["Color03B"] = [System.Windows.Media.SolidColorBrush]::new([System.Windows.Media.Color]::FromArgb(255, 218, 220, 224))
    $app.Resources["Color04B"] = [System.Windows.Media.SolidColorBrush]::new([System.Windows.Media.Color]::FromArgb(255, 100, 105, 115))
    $app.Resources["Color05B"] = [System.Windows.Media.SolidColorBrush]::new([System.Windows.Media.Color]::FromArgb(255, 28, 30, 33))
    $app.Resources["Color10B"] = [System.Windows.Media.SolidColorBrush]::new([System.Windows.Media.Color]::FromArgb(255, 219, 58, 58))
    $app.Resources["BasicSystemAccentColor"] = [System.Windows.Media.SolidColorBrush]::new([System.Windows.Media.Color]::FromArgb(255, 0, 103, 192))
    $app.Resources["SettingPanelMargin"] = [System.Windows.Thickness]::new(10)
    $app.Resources["SettingPanelItemTopBottomMargin"] = [System.Windows.Thickness]::new(0, 0, 0, 12)

    # Accent button style
    $btnStyle = [System.Windows.Style]::new([System.Windows.Controls.Button])
    $btnStyle.Setters.Add([System.Windows.Setter]::new([System.Windows.Controls.Button]::BackgroundProperty, $app.Resources["BasicSystemAccentColor"]))
    $btnStyle.Setters.Add([System.Windows.Setter]::new([System.Windows.Controls.Button]::ForegroundProperty, [System.Windows.Media.Brushes]::White))
    $app.Resources["AccentButtonStyle"] = $btnStyle

    # Temporary directory with demo snippets
    $tempDir = [System.IO.Path]::Combine([System.IO.Path]::GetTempPath(), "SnippetCapture_" + [System.Guid]::NewGuid().ToString("N"))
    [System.IO.Directory]::CreateDirectory($tempDir) | Out-Null

    try {
        $store = [Flow.Launcher.Plugin.Snippets.Storage.SnippetStore]::new($tempDir)
        $store.Add([Flow.Launcher.Plugin.Snippets.Models.Snippet]::new("git-cm", 'git commit -m "$message$"', 100))
        $store.Add([Flow.Launcher.Plugin.Snippets.Models.Snippet]::new("pr-template", "## Summary`n`n- Description of changes`n`n## Verification`n- [x] Tested locally", 80))
        $store.Add([Flow.Launcher.Plugin.Snippets.Models.Snippet]::new("docker-compose", "version: '3.8'`nservices:`n  app:`n    image: node:20`n    ports:`n      - '3000:3000'", 50))
        $store.Add([Flow.Launcher.Plugin.Snippets.Models.Snippet]::new("email-sig", "Best regards,`nJohn Doe`nSoftware Engineer", 30))

        # ----------------------------------------------------
        # 1. Capture SnippetManagerWindow (manager.png)
        # ----------------------------------------------------
        $window = [Flow.Launcher.Plugin.Snippets.Views.SnippetManagerWindow]::new($store, $null)
        $window.Width = 920
        $window.Height = 560
        $window.Show()

        # Select the first snippet to show details in the editor
        if ($window.SnippetsGrid.Items.Count -gt 0) {
            $window.SnippetsGrid.SelectedIndex = 0
        }
        $window.UpdateLayout()

        $managerPng = [System.IO.Path]::Combine($outputDir, "manager.png")
        Save-VisualToPng $window 920 560 $managerPng
        $window.Close()

        # ----------------------------------------------------
        # 2. Capture SettingsControl (settings.png)
        # ----------------------------------------------------
        $settingsControl = [Flow.Launcher.Plugin.Snippets.Views.SettingsControl]::new($store)
        
        $settingsFrame = [System.Windows.Controls.Border]::new()
        $settingsFrame.Background = $app.Resources["Color01B"]
        $settingsFrame.BorderBrush = $app.Resources["Color03B"]
        $settingsFrame.BorderThickness = [System.Windows.Thickness]::new(1)
        $settingsFrame.CornerRadius = [System.Windows.CornerRadius]::new(8)
        $settingsFrame.Padding = [System.Windows.Thickness]::new(24, 20, 24, 20)
        $settingsFrame.Child = $settingsControl

        $settingsPng = [System.IO.Path]::Combine($outputDir, "settings.png")
        Save-VisualToPng $settingsFrame 680 150 $settingsPng

        # ----------------------------------------------------
        # 3. Capture Flow Launcher Search Mockup (search.png)
        # ----------------------------------------------------
        $searchWindow = [System.Windows.Controls.Border]::new()
        $searchWindow.Width = 640
        $searchWindow.Background = [System.Windows.Media.Brushes]::White
        $searchWindow.BorderBrush = $app.Resources["Color03B"]
        $searchWindow.BorderThickness = [System.Windows.Thickness]::new(1)
        $searchWindow.CornerRadius = [System.Windows.CornerRadius]::new(10)
        $searchWindow.Effect = (New-Object System.Windows.Media.Effects.DropShadowEffect -Property @{
            BlurRadius = 16
            ShadowDepth = 3
            Opacity = 0.15
            Color = [System.Windows.Media.Colors]::Black
        })

        $searchStack = [System.Windows.Controls.StackPanel]::new()
        $searchStack.Margin = [System.Windows.Thickness]::new(12)

        # Search Bar
        $searchBar = [System.Windows.Controls.Grid]::new()
        $col1 = [System.Windows.Controls.ColumnDefinition]::new()
        $col1.Width = [System.Windows.GridLength]::Auto
        $col2 = [System.Windows.Controls.ColumnDefinition]::new()
        $col2.Width = [System.Windows.GridLength]::new(1, [System.Windows.GridUnitType]::Star)
        $searchBar.ColumnDefinitions.Add($col1)
        $searchBar.ColumnDefinitions.Add($col2)

        $logoPath = [System.IO.Path]::GetFullPath("$scriptRoot/../Flow.Launcher.Plugin.Snippets/Images/Snippets.png")
        $logoImg = [System.Windows.Controls.Image]::new()
        if (Test-Path $logoPath) {
            $logoImg.Source = [System.Windows.Media.Imaging.BitmapImage]::new([System.Uri]::new($logoPath))
        }
        $logoImg.Width = 28
        $logoImg.Height = 28
        $logoImg.Margin = [System.Windows.Thickness]::new(6, 0, 12, 0)
        [System.Windows.Controls.Grid]::SetColumn($logoImg, 0)
        $searchBar.Children.Add($logoImg) | Out-Null

        $queryBox = [System.Windows.Controls.TextBlock]::new()
        $queryBox.Text = "sp git"
        $queryBox.FontSize = 18
        $queryBox.FontWeight = [System.Windows.FontWeights]::SemiBold
        $queryBox.Foreground = $app.Resources["Color05B"]
        $queryBox.VerticalAlignment = [System.Windows.VerticalAlignment]::Center
        [System.Windows.Controls.Grid]::SetColumn($queryBox, 1)
        $searchBar.Children.Add($queryBox) | Out-Null
        $searchStack.Children.Add($searchBar) | Out-Null

        # Divider
        $divider = [System.Windows.Controls.Border]::new()
        $divider.Height = 1
        $divider.Background = $app.Resources["Color03B"]
        $divider.Margin = [System.Windows.Thickness]::new(0, 10, 0, 8)
        $searchStack.Children.Add($divider) | Out-Null

        # Results list
        $resultItems = @(
            @{ Key = "git-cm"; Value = 'git commit -m "$message$"'; Score = 100; Selected = $true },
            @{ Key = "git-pull"; Value = "git pull --rebase origin main"; Score = 90; Selected = $false },
            @{ Key = "git-stash"; Value = "git stash && git pull && git stash pop"; Score = 70; Selected = $false }
        )

        foreach ($item in $resultItems) {
            $rowBorder = [System.Windows.Controls.Border]::new()
            $rowBorder.CornerRadius = [System.Windows.CornerRadius]::new(6)
            $rowBorder.Padding = [System.Windows.Thickness]::new(10, 8, 10, 8)
            $rowBorder.Margin = [System.Windows.Thickness]::new(0, 2, 0, 2)
            
            if ($item.Selected) {
                $rowBorder.Background = $app.Resources["Color02B"]
                $rowBorder.BorderBrush = $app.Resources["BasicSystemAccentColor"]
                $rowBorder.BorderThickness = [System.Windows.Thickness]::new(2, 0, 0, 0)
            } else {
                $rowBorder.Background = [System.Windows.Media.Brushes]::Transparent
            }

            $rowGrid = [System.Windows.Controls.Grid]::new()
            $rc1 = [System.Windows.Controls.ColumnDefinition]::new()
            $rc1.Width = [System.Windows.GridLength]::Auto
            $rc2 = [System.Windows.Controls.ColumnDefinition]::new()
            $rc2.Width = [System.Windows.GridLength]::new(1, [System.Windows.GridUnitType]::Star)
            $rc3 = [System.Windows.Controls.ColumnDefinition]::new()
            $rc3.Width = [System.Windows.GridLength]::Auto
            $rowGrid.ColumnDefinitions.Add($rc1)
            $rowGrid.ColumnDefinitions.Add($rc2)
            $rowGrid.ColumnDefinitions.Add($rc3)

            $rIcon = [System.Windows.Controls.Image]::new()
            if (Test-Path $logoPath) {
                $rIcon.Source = [System.Windows.Media.Imaging.BitmapImage]::new([System.Uri]::new($logoPath))
            }
            $rIcon.Width = 22
            $rIcon.Height = 22
            $rIcon.Margin = [System.Windows.Thickness]::new(0, 0, 10, 0)
            [System.Windows.Controls.Grid]::SetColumn($rIcon, 0)
            $rowGrid.Children.Add($rIcon) | Out-Null

            $textStack = [System.Windows.Controls.StackPanel]::new()
            $titleBlock = [System.Windows.Controls.TextBlock]::new()
            $titleBlock.Text = $item.Key
            $titleBlock.FontWeight = [System.Windows.FontWeights]::SemiBold
            $titleBlock.FontSize = 13
            $titleBlock.Foreground = $app.Resources["Color05B"]

            $subBlock = [System.Windows.Controls.TextBlock]::new()
            $subBlock.Text = $item.Value
            $subBlock.FontSize = 11
            $subBlock.Foreground = $app.Resources["Color04B"]
            $subBlock.TextTrimming = [System.Windows.TextTrimming]::CharacterEllipsis

            $textStack.Children.Add($titleBlock) | Out-Null
            $textStack.Children.Add($subBlock) | Out-Null
            [System.Windows.Controls.Grid]::SetColumn($textStack, 1)
            $rowGrid.Children.Add($textStack) | Out-Null

            $scoreBlock = [System.Windows.Controls.TextBlock]::new()
            $scoreBlock.Text = "$($item.Score) pts"
            $scoreBlock.FontSize = 11
            $scoreBlock.Foreground = $app.Resources["Color04B"]
            $scoreBlock.VerticalAlignment = [System.Windows.VerticalAlignment]::Center
            [System.Windows.Controls.Grid]::SetColumn($scoreBlock, 2)
            $rowGrid.Children.Add($scoreBlock) | Out-Null

            $rowBorder.Child = $rowGrid
            $searchStack.Children.Add($rowBorder) | Out-Null
        }

        $searchWindow.Child = $searchStack

        $searchPng = [System.IO.Path]::Combine($outputDir, "search.png")
        Save-VisualToPng $searchWindow 640 230 $searchPng

    } finally {
        if (Test-Path $tempDir) {
            [System.IO.Directory]::Delete($tempDir, $true)
        }
    }
}

# Invoke directly on current STA thread
& $code $dllPath $resolvedOutputDir $PSScriptRoot

Write-Host "All screenshots captured successfully!"
