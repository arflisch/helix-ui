"""Templates of the host (dialogs, drawers, confirmations, prompts), navigation and menus (appended to Controls.axaml)."""

ANIM = '''<Style.Animations>
                                    <Animation Duration="0:0:0.16" Easing="CubicEaseOut" FillMode="Forward">
                                        <KeyFrame Cue="0%"><Setter Property="Opacity" Value="0" /><Setter Property="ScaleTransform.ScaleX" Value="0.97" /><Setter Property="ScaleTransform.ScaleY" Value="0.97" /></KeyFrame>
                                        <KeyFrame Cue="100%"><Setter Property="Opacity" Value="1" /><Setter Property="ScaleTransform.ScaleX" Value="1" /><Setter Property="ScaleTransform.ScaleY" Value="1" /></KeyFrame>
                                    </Animation>
                                </Style.Animations>'''

def backdrop(command):
    return f'''<Button Theme="{{StaticResource HelixBackdrop}}" Command="{{Binding {command}}}" />'''

def box_header(title, description, close_command, close_visible="True"):
    return f'''<DockPanel DockPanel.Dock="Top" Margin="24,20,16,0">
                                            <Button DockPanel.Dock="Right" Theme="{{StaticResource HelixIconButton}}" VerticalAlignment="Top" Command="{{Binding {close_command}}}"
                                                    IsVisible="{close_visible}" ToolTip.Tip="{{x:Static h:HelixHost.CloseText}}">
                                                <h:HelixIcon Icon="x" Size="16" />
                                            </Button>
                                            <StackPanel Spacing="4" Margin="0,0,8,0">
                                                <TextBlock Text="{{Binding {title}}}" FontSize="17" FontWeight="SemiBold" TextWrapping="Wrap" />
                                                <TextBlock Text="{{Binding {description}}}" FontSize="13.5" Foreground="{{DynamicResource Helix.TextMuted}}" TextWrapping="Wrap"
                                                           IsVisible="{{Binding {description}, Converter={{x:Static StringConverters.IsNotNullOrEmpty}}}}" />
                                            </StackPanel>
                                        </DockPanel>'''

BOX = 'Background="{DynamicResource Helix.Surface}" BorderBrush="{DynamicResource Helix.Border}" BorderThickness="1" CornerRadius="16" BoxShadow="0 24 64 0 #40000000" HorizontalAlignment="Center" VerticalAlignment="Center" Margin="24" RenderTransformOrigin="50%,50%"'

def build():
    return f'''
    <!-- ===================================================================== Host: dialogs, drawers, confirmations, prompts -->

    <ControlTheme x:Key="HelixBackdrop" TargetType="Button">
        <Setter Property="Background" Value="{{DynamicResource Helix.Overlay}}" />
        <Setter Property="HorizontalAlignment" Value="Stretch" />
        <Setter Property="VerticalAlignment" Value="Stretch" />
        <Setter Property="Focusable" Value="False" />
        <Setter Property="Template">
            <ControlTemplate><Border Background="{{TemplateBinding Background}}" /></ControlTemplate>
        </Setter>
    </ControlTheme>

    <ControlTheme x:Key="{{x:Type h:HelixDialog}}" TargetType="h:HelixDialog">
        <!-- The dialog shows nothing where it is declared: the HelixHost draws it (content and footer) above the window. -->
        <Setter Property="Template"><ControlTemplate><Panel /></ControlTemplate></Setter>
    </ControlTheme>
    <ControlTheme x:Key="{{x:Type h:HelixDrawer}}" TargetType="h:HelixDrawer" BasedOn="{{StaticResource {{x:Type h:HelixDialog}}}}" />

    <ControlTheme x:Key="{{x:Type h:HelixHost}}" TargetType="h:HelixHost">
        <Setter Property="Template">
            <ControlTemplate>
                <Panel>
                    <ContentPresenter Name="PART_ContentPresenter" Content="{{TemplateBinding Content}}" ContentTemplate="{{TemplateBinding ContentTemplate}}" />
                    <ItemsControl ItemsSource="{{Binding Overlays, RelativeSource={{RelativeSource TemplatedParent}}}}" IsVisible="{{TemplateBinding HasOverlay}}"
                                  TextElement.FontFamily="{{DynamicResource Helix.FontFamily}}" TextElement.FontSize="{{DynamicResource Helix.FontSize}}"
                                  TextElement.Foreground="{{DynamicResource Helix.Text}}">
                        <ItemsControl.ItemsPanel><ItemsPanelTemplate><Panel /></ItemsPanelTemplate></ItemsControl.ItemsPanel>
                        <ItemsControl.DataTemplates>
                            <DataTemplate DataType="h:HelixDrawerSlot">
                                <Panel>
                                    {backdrop("Dialog.BackdropCommand")}
                                    <Border Classes="drawer" Classes.left="{{Binding IsLeft}}" Classes.bottom="{{Binding IsBottom}}" Background="{{DynamicResource Helix.Surface}}"
                                            BorderBrush="{{DynamicResource Helix.Border}}" BoxShadow="0 0 48 0 #40000000">
                                        <Border.Styles>
                                            <Style Selector="Border.drawer">
                                                <Setter Property="HorizontalAlignment" Value="Right" />
                                                <Setter Property="Width" Value="{{Binding Drawer.DrawerSize}}" />
                                                <Setter Property="BorderThickness" Value="1,0,0,0" />
                                                <Setter Property="MaxWidth" Value="{{Binding $parent[Panel].Bounds.Width}}" />
                                            </Style>
                                            <Style Selector="Border.drawer.left">
                                                <Setter Property="HorizontalAlignment" Value="Left" />
                                                <Setter Property="BorderThickness" Value="0,0,1,0" />
                                            </Style>
                                            <Style Selector="Border.drawer.bottom">
                                                <Setter Property="HorizontalAlignment" Value="Stretch" />
                                                <Setter Property="VerticalAlignment" Value="Bottom" />
                                                <Setter Property="Width" Value="NaN" />
                                                <Setter Property="Height" Value="{{Binding Drawer.DrawerSize}}" />
                                                <Setter Property="BorderThickness" Value="0,1,0,0" />
                                                <Setter Property="CornerRadius" Value="16,16,0,0" />
                                            </Style>
                                            <Style Selector="Border.drawer">
                                                <Style.Animations>
                                                    <Animation Duration="0:0:0.18" Easing="CubicEaseOut" FillMode="Forward">
                                                        <KeyFrame Cue="0%"><Setter Property="Opacity" Value="0.4" /><Setter Property="TranslateTransform.X" Value="40" /></KeyFrame>
                                                        <KeyFrame Cue="100%"><Setter Property="Opacity" Value="1" /><Setter Property="TranslateTransform.X" Value="0" /></KeyFrame>
                                                    </Animation>
                                                </Style.Animations>
                                            </Style>
                                        </Border.Styles>
                                        <DockPanel>
                                            {box_header("Dialog.Title", "Dialog.Description", "Dialog.CloseCommand", "{Binding Dialog.ShowClose}")}
                                            <Border DockPanel.Dock="Bottom" BorderBrush="{{DynamicResource Helix.Border}}" BorderThickness="0,1,0,0" Padding="24,14"
                                                    IsVisible="{{Binding Dialog.Footer, Converter={{x:Static ObjectConverters.IsNotNull}}}}">
                                                <ContentPresenter Content="{{Binding Dialog.Footer}}" HorizontalAlignment="Right" />
                                            </Border>
                                            <ScrollViewer><ContentPresenter Content="{{Binding Dialog.Content}}" Margin="24,16,24,20" /></ScrollViewer>
                                        </DockPanel>
                                    </Border>
                                </Panel>
                            </DataTemplate>
                            <DataTemplate DataType="h:HelixDialogSlot">
                                <Panel>
                                    {backdrop("Dialog.BackdropCommand")}
                                    <Border Classes="box" Width="{{Binding Dialog.DialogWidth}}" {BOX}>
                                        <Border.Styles><Style Selector="Border.box">{ANIM}</Style></Border.Styles>
                                        <DockPanel>
                                            {box_header("Dialog.Title", "Dialog.Description", "Dialog.CloseCommand", "{Binding Dialog.ShowClose}")}
                                            <Border DockPanel.Dock="Bottom" Padding="24,0,24,20" IsVisible="{{Binding Dialog.Footer, Converter={{x:Static ObjectConverters.IsNotNull}}}}">
                                                <ContentPresenter Content="{{Binding Dialog.Footer}}" HorizontalAlignment="Right" />
                                            </Border>
                                            <ContentPresenter Content="{{Binding Dialog.Content}}" Margin="24,14,24,20" />
                                        </DockPanel>
                                    </Border>
                                </Panel>
                            </DataTemplate>
                            <DataTemplate DataType="h:HelixConfirmRequest">
                                <Panel>
                                    {backdrop("CancelCommand")}
                                    <Border Classes="box" Width="420" {BOX} Padding="24,22,24,20">
                                        <Border.Styles><Style Selector="Border.box">{ANIM}</Style></Border.Styles>
                                        <StackPanel Spacing="18">
                                            <DockPanel>
                                                <Panel DockPanel.Dock="Left" Width="40" Height="40" VerticalAlignment="Top" Margin="0,0,14,0">
                                                    <Ellipse Classes.danger="{{Binding IsDanger}}" Opacity="0.14">
                                                        <Ellipse.Styles>
                                                            <Style Selector="Ellipse"><Setter Property="Fill" Value="{{DynamicResource Helix.Primary}}" /></Style>
                                                            <Style Selector="Ellipse.danger"><Setter Property="Fill" Value="{{DynamicResource Helix.Danger}}" /></Style>
                                                        </Ellipse.Styles>
                                                    </Ellipse>
                                                    <h:HelixIcon Icon="{{Binding Icon}}" Size="20" HorizontalAlignment="Center" VerticalAlignment="Center" Classes.danger="{{Binding IsDanger}}">
                                                        <h:HelixIcon.Styles>
                                                            <Style Selector="h|HelixIcon"><Setter Property="Foreground" Value="{{DynamicResource Helix.Primary}}" /></Style>
                                                            <Style Selector="h|HelixIcon.danger"><Setter Property="Foreground" Value="{{DynamicResource Helix.Danger}}" /></Style>
                                                        </h:HelixIcon.Styles>
                                                    </h:HelixIcon>
                                                </Panel>
                                                <StackPanel Spacing="6">
                                                    <TextBlock Text="{{Binding Title}}" FontSize="16.5" FontWeight="SemiBold" TextWrapping="Wrap" />
                                                    <TextBlock Text="{{Binding Message}}" Foreground="{{DynamicResource Helix.TextMuted}}" TextWrapping="Wrap" IsVisible="{{Binding HasMessage}}" />
                                                </StackPanel>
                                            </DockPanel>
                                            <StackPanel Orientation="Horizontal" Spacing="10" HorizontalAlignment="Right">
                                                <h:HelixButton Content="{{Binding CancelText}}" Variant="Outline" Tone="Neutral" Command="{{Binding CancelCommand}}" />
                                                <h:HelixButton Content="{{Binding ConfirmText}}" Tone="{{Binding ConfirmTone}}" Command="{{Binding ConfirmCommand}}" />
                                            </StackPanel>
                                        </StackPanel>
                                    </Border>
                                </Panel>
                            </DataTemplate>
                            <DataTemplate DataType="h:HelixPromptRequest">
                                <Panel>
                                    {backdrop("CancelCommand")}
                                    <Border Classes="box" Width="440" {BOX} Padding="24,22,24,20">
                                        <Border.Styles><Style Selector="Border.box">{ANIM}</Style></Border.Styles>
                                        <StackPanel Spacing="14">
                                            <StackPanel Spacing="6">
                                                <TextBlock Text="{{Binding Title}}" FontSize="16.5" FontWeight="SemiBold" TextWrapping="Wrap" />
                                                <TextBlock Text="{{Binding Message}}" Foreground="{{DynamicResource Helix.TextMuted}}" TextWrapping="Wrap" IsVisible="{{Binding HasMessage}}" />
                                            </StackPanel>
                                            <h:HelixTextField Name="PromptField" Text="{{Binding Text, Mode=TwoWay}}" Placeholder="{{Binding Placeholder}}" Error="{{Binding Error}}">
                                                <h:HelixTextField.KeyBindings>
                                                    <KeyBinding Gesture="Enter" Command="{{Binding ConfirmCommand}}" />
                                                </h:HelixTextField.KeyBindings>
                                            </h:HelixTextField>
                                            <StackPanel Orientation="Horizontal" Spacing="10" HorizontalAlignment="Right">
                                                <h:HelixButton Content="{{Binding CancelText}}" Variant="Outline" Tone="Neutral" Command="{{Binding CancelCommand}}" />
                                                <h:HelixButton Content="{{Binding ConfirmText}}" Command="{{Binding ConfirmCommand}}" />
                                            </StackPanel>
                                        </StackPanel>
                                    </Border>
                                </Panel>
                            </DataTemplate>
                            <DataTemplate DataType="h:HelixViewRequest">
                                <Panel>
                                    <Border Background="{{DynamicResource Helix.Overlay}}" />
                                    <Border Classes="box" Width="{{Binding DialogWidth}}" {BOX}>
                                        <Border.Styles><Style Selector="Border.box">{ANIM}</Style></Border.Styles>
                                        <DockPanel>
                                            {box_header("Title", "Description", "CloseCommand")}
                                            <ContentPresenter Content="{{Binding View}}" Margin="24,14,24,20" />
                                        </DockPanel>
                                    </Border>
                                </Panel>
                            </DataTemplate>
                        </ItemsControl.DataTemplates>
                    </ItemsControl>
                    <h:HelixToastHost ToastHorizontalAlignment="{{TemplateBinding ToastHorizontalAlignment}}" ToastVerticalAlignment="{{TemplateBinding ToastVerticalAlignment}}" />
                </Panel>
            </ControlTemplate>
        </Setter>
    </ControlTheme>

    <!-- ===================================================================== Tabs -->

    <ControlTheme x:Key="{{x:Type h:HelixTab}}" TargetType="h:HelixTab">
        <Setter Property="FontSize" Value="14" />
        <Setter Property="FontWeight" Value="Medium" />
        <Setter Property="Foreground" Value="{{DynamicResource Helix.TextMuted}}" />
        <Setter Property="Background" Value="Transparent" />
        <Setter Property="Cursor" Value="Hand" />
        <Setter Property="Template">
            <ControlTemplate>
                <Panel>
                    <Border Name="Bg" Background="{{TemplateBinding Background}}" CornerRadius="8" Padding="12,8">
                        <StackPanel Orientation="Horizontal" Spacing="7">
                            <h:HelixIcon Icon="{{TemplateBinding Icon}}" Size="15" VerticalAlignment="Center" IsVisible="{{TemplateBinding Icon, Converter={{x:Static ObjectConverters.IsNotNull}}}}" />
                            <ContentPresenter Name="PART_ContentPresenter" Content="{{TemplateBinding Header}}" ContentTemplate="{{TemplateBinding HeaderTemplate}}" VerticalAlignment="Center" />
                        </StackPanel>
                    </Border>
                    <Border Name="Line" Height="2" CornerRadius="1" VerticalAlignment="Bottom" Margin="8,0,8,-1" Background="{{DynamicResource Helix.Primary}}" Opacity="0" />
                </Panel>
            </ControlTemplate>
        </Setter>
        <Style Selector="^:pointerover"><Setter Property="Foreground" Value="{{DynamicResource Helix.Text}}" /></Style>
        <Style Selector="^:selected"><Setter Property="Foreground" Value="{{DynamicResource Helix.Text}}" /><Setter Property="FontWeight" Value="SemiBold" /></Style>
        <Style Selector="^:selected /template/ Border#Line"><Setter Property="Opacity" Value="1" /></Style>
        <Style Selector="^:disabled"><Setter Property="Opacity" Value="0.45" /></Style>
        <Style Selector="^[Variant=Pills] /template/ Border#Line"><Setter Property="IsVisible" Value="False" /></Style>
        <Style Selector="^[Variant=Pills]:selected">
            <Setter Property="Background" Value="{{DynamicResource Helix.PrimarySoft}}" />
            <Setter Property="Foreground" Value="{{DynamicResource Helix.Primary}}" />
        </Style>
        <Style Selector="^[Variant=Segmented] /template/ Border#Line"><Setter Property="IsVisible" Value="False" /></Style>
        <Style Selector="^[Variant=Segmented] /template/ Border#Bg"><Setter Property="Padding" Value="14,6" /></Style>
        <Style Selector="^[Variant=Segmented]:selected"><Setter Property="Background" Value="{{DynamicResource Helix.Surface}}" /></Style>
        <Style Selector="^[Variant=Segmented]:selected /template/ Border#Bg"><Setter Property="BoxShadow" Value="0 1 3 0 #24000000" /></Style>
    </ControlTheme>

    <ControlTheme x:Key="{{x:Type h:HelixTabs}}" TargetType="h:HelixTabs">
        <Setter Property="FontFamily" Value="{{DynamicResource Helix.FontFamily}}" />
        <Setter Property="Foreground" Value="{{DynamicResource Helix.Text}}" />
        <Setter Property="ItemsPanel"><ItemsPanelTemplate><StackPanel Orientation="Horizontal" Spacing="2" /></ItemsPanelTemplate></Setter>
        <Setter Property="Template">
            <ControlTemplate>
                <DockPanel>
                    <Border Name="Strip" DockPanel.Dock="Top" BorderBrush="{{DynamicResource Helix.Border}}" BorderThickness="0,0,0,1" HorizontalAlignment="Stretch">
                        <ItemsPresenter Name="PART_ItemsPresenter" ItemsPanel="{{TemplateBinding ItemsPanel}}" />
                    </Border>
                    <ContentPresenter Name="PART_SelectedContentHost" Content="{{TemplateBinding SelectedContent}}" ContentTemplate="{{TemplateBinding SelectedContentTemplate}}" Margin="0,16,0,0" />
                </DockPanel>
            </ControlTemplate>
        </Setter>
        <Style Selector="^[Variant=Pills] /template/ Border#Strip"><Setter Property="BorderThickness" Value="0" /></Style>
        <Style Selector="^[Variant=Segmented] /template/ Border#Strip">
            <Setter Property="BorderThickness" Value="0" />
            <Setter Property="Background" Value="{{DynamicResource Helix.Surface2}}" />
            <Setter Property="CornerRadius" Value="10" />
            <Setter Property="Padding" Value="3" />
            <Setter Property="HorizontalAlignment" Value="Left" />
        </Style>
    </ControlTheme>

    <!-- ===================================================================== Accordion -->

    <ControlTheme x:Key="{{x:Type h:HelixAccordionItem}}" TargetType="h:HelixAccordionItem">
        <Setter Property="Foreground" Value="{{DynamicResource Helix.Text}}" />
        <Setter Property="Template">
            <ControlTemplate>
                <Border Name="Line" BorderBrush="{{DynamicResource Helix.Border}}" BorderThickness="0,0,0,1">
                    <DockPanel>
                        <ToggleButton DockPanel.Dock="Top" Theme="{{StaticResource HelixBareToggle}}" IsChecked="{{TemplateBinding IsExpanded, Mode=TwoWay}}"
                                      HorizontalAlignment="Stretch" HorizontalContentAlignment="Stretch" Padding="16,14">
                            <DockPanel>
                                <h:HelixIcon Name="Chevron" DockPanel.Dock="Right" Icon="chevron-down" Size="16" Foreground="{{DynamicResource Helix.TextMuted}}" VerticalAlignment="Center" Margin="10,0,0,0">
                                    <h:HelixIcon.Transitions><Transitions><TransformOperationsTransition Property="RenderTransform" Duration="0:0:0.15" /></Transitions></h:HelixIcon.Transitions>
                                </h:HelixIcon>
                                <ContentPresenter DockPanel.Dock="Right" Content="{{TemplateBinding Extra}}" VerticalAlignment="Center" />
                                <h:HelixIcon DockPanel.Dock="Left" Icon="{{TemplateBinding Icon}}" Size="18" Foreground="{{DynamicResource Helix.TextMuted}}" VerticalAlignment="Center"
                                             Margin="0,0,12,0" IsVisible="{{TemplateBinding Icon, Converter={{x:Static ObjectConverters.IsNotNull}}}}" />
                                <StackPanel Spacing="2" VerticalAlignment="Center">
                                    <ContentPresenter Content="{{TemplateBinding Header}}" FontWeight="SemiBold" />
                                    <TextBlock Text="{{TemplateBinding Subtitle}}" FontSize="12.5" Foreground="{{DynamicResource Helix.TextMuted}}"
                                               IsVisible="{{TemplateBinding Subtitle, Converter={{x:Static StringConverters.IsNotNullOrEmpty}}}}" />
                                </StackPanel>
                            </DockPanel>
                        </ToggleButton>
                        <ContentPresenter Name="PART_ContentPresenter" Content="{{TemplateBinding Content}}" ContentTemplate="{{TemplateBinding ContentTemplate}}"
                                          Padding="16,0,16,16" Foreground="{{DynamicResource Helix.TextMuted}}" IsVisible="{{TemplateBinding IsExpanded}}" />
                    </DockPanel>
                </Border>
            </ControlTemplate>
        </Setter>
        <Style Selector="^:expanded /template/ h|HelixIcon#Chevron"><Setter Property="RenderTransform" Value="rotate(180deg)" /></Style>
        <Style Selector="^:nth-last-child(1) /template/ Border#Line"><Setter Property="BorderThickness" Value="0" /></Style>
    </ControlTheme>

    <ControlTheme x:Key="{{x:Type h:HelixAccordion}}" TargetType="h:HelixAccordion">
        <Setter Property="FontFamily" Value="{{DynamicResource Helix.FontFamily}}" />
        <Setter Property="FontSize" Value="{{DynamicResource Helix.FontSize}}" />
        <Setter Property="Template">
            <ControlTemplate>
                <Border Name="Frame" Background="{{DynamicResource Helix.Surface}}" BorderBrush="{{DynamicResource Helix.Border}}" BorderThickness="1" CornerRadius="12" ClipToBounds="True">
                    <ItemsPresenter Name="PART_ItemsPresenter" ItemsPanel="{{TemplateBinding ItemsPanel}}" />
                </Border>
            </ControlTemplate>
        </Setter>
        <Style Selector="^[IsFlush=True] /template/ Border#Frame">
            <Setter Property="BorderThickness" Value="0" />
            <Setter Property="Background" Value="Transparent" />
        </Style>
    </ControlTheme>

    <!-- ===================================================================== Breadcrumb, button group, pagination, stepper -->

    <ControlTheme x:Key="{{x:Type h:HelixBreadcrumb}}" TargetType="h:HelixBreadcrumb">
        <Setter Property="Template"><ControlTemplate><WrapPanel Name="PART_Panel" /></ControlTemplate></Setter>
    </ControlTheme>

    <ControlTheme x:Key="HelixButtonGroupItem" TargetType="ListBoxItem">
        <Setter Property="Foreground" Value="{{DynamicResource Helix.TextMuted}}" />
        <Setter Property="Background" Value="Transparent" />
        <Setter Property="FontWeight" Value="Medium" />
        <Setter Property="Cursor" Value="Hand" />
        <Setter Property="Template">
            <ControlTemplate>
                <Border Name="Bg" Background="{{TemplateBinding Background}}" BorderBrush="{{DynamicResource Helix.BorderStrong}}" BorderThickness="1,0,0,0" Padding="14,0" MinHeight="36">
                    <ContentPresenter Name="PART_ContentPresenter" Content="{{TemplateBinding Content}}" ContentTemplate="{{TemplateBinding ContentTemplate}}" VerticalAlignment="Center" />
                </Border>
            </ControlTemplate>
        </Setter>
        <Style Selector="^:nth-child(1) /template/ Border#Bg"><Setter Property="BorderThickness" Value="0" /></Style>
        <Style Selector="^:pointerover"><Setter Property="Background" Value="{{DynamicResource Helix.Surface2}}" /><Setter Property="Foreground" Value="{{DynamicResource Helix.Text}}" /></Style>
        <Style Selector="^:selected">
            <Setter Property="Background" Value="{{DynamicResource Helix.PrimarySoft}}" />
            <Setter Property="Foreground" Value="{{DynamicResource Helix.Primary}}" />
            <Setter Property="FontWeight" Value="SemiBold" />
        </Style>
    </ControlTheme>

    <ControlTheme x:Key="{{x:Type h:HelixButtonGroup}}" TargetType="h:HelixButtonGroup">
        <Setter Property="FontFamily" Value="{{DynamicResource Helix.FontFamily}}" />
        <Setter Property="FontSize" Value="{{DynamicResource Helix.FontSize}}" />
        <Setter Property="ItemContainerTheme" Value="{{StaticResource HelixButtonGroupItem}}" />
        <Setter Property="SelectionMode" Value="Single,AlwaysSelected" />
        <Setter Property="HorizontalAlignment" Value="Left" />
        <Setter Property="ItemsPanel"><ItemsPanelTemplate><StackPanel Orientation="Horizontal" /></ItemsPanelTemplate></Setter>
        <Setter Property="Template">
            <ControlTemplate>
                <Border Background="{{DynamicResource Helix.Surface}}" BorderBrush="{{DynamicResource Helix.BorderStrong}}" BorderThickness="1" CornerRadius="8" ClipToBounds="True">
                    <ItemsPresenter Name="PART_ItemsPresenter" ItemsPanel="{{TemplateBinding ItemsPanel}}" />
                </Border>
            </ControlTemplate>
        </Setter>
    </ControlTheme>

    <ControlTheme x:Key="{{x:Type h:HelixPagination}}" TargetType="h:HelixPagination">
        <Setter Property="FontFamily" Value="{{DynamicResource Helix.FontFamily}}" />
        <Setter Property="Template"><ControlTemplate><StackPanel Name="PART_Pages" Orientation="Horizontal" /></ControlTemplate></Setter>
    </ControlTheme>

    <ControlTheme x:Key="{{x:Type h:HelixStepper}}" TargetType="h:HelixStepper">
        <Setter Property="Accent" Value="{{DynamicResource Helix.Primary}}" />
        <Setter Property="ToneOnBrush" Value="{{DynamicResource Helix.OnPrimary}}" />
        <Setter Property="FontFamily" Value="{{DynamicResource Helix.FontFamily}}" />
        <Setter Property="FontSize" Value="{{DynamicResource Helix.FontSize}}" />
        <Setter Property="Template"><ControlTemplate><Grid Name="PART_Steps" /></ControlTemplate></Setter>
''' + "\n".join(f'''        <Style Selector="^[Tone={t}]"><Setter Property="Accent" Value="{{DynamicResource Helix.{t}}}" /><Setter Property="ToneOnBrush" Value="{{DynamicResource Helix.On{t}}}" /></Style>''' for t in ["Neutral","Success","Warning","Danger","Info"]) + f'''
    </ControlTheme>

    <!-- ===================================================================== Avatar, skeleton, splitter -->

    <ControlTheme x:Key="{{x:Type h:HelixAvatar}}" TargetType="h:HelixAvatar">
        <Setter Property="Width" Value="36" />
        <Setter Property="Height" Value="36" />
        <Setter Property="FontSize" Value="13" />
        <Setter Property="FontFamily" Value="{{DynamicResource Helix.FontFamily}}" />
        <Setter Property="Template">
            <ControlTemplate>
                <Panel>
                    <Border CornerRadius="999" BorderThickness="2" BorderBrush="{{DynamicResource Helix.Surface}}" Background="{{TemplateBinding Fill}}" ClipToBounds="True">
                        <Panel>
                            <TextBlock Text="{{TemplateBinding Initials}}" Foreground="White" FontWeight="SemiBold" HorizontalAlignment="Center" VerticalAlignment="Center" />
                            <Image Source="{{TemplateBinding Source}}" Stretch="UniformToFill" IsVisible="{{TemplateBinding Source, Converter={{x:Static ObjectConverters.IsNotNull}}}}" />
                        </Panel>
                    </Border>
                    <Ellipse Name="Status" Width="10" Height="10" HorizontalAlignment="Right" VerticalAlignment="Bottom" Stroke="{{DynamicResource Helix.Surface}}" StrokeThickness="2" IsVisible="False" />
                </Panel>
            </ControlTemplate>
        </Setter>
        <Style Selector="^[Size=Sm]"><Setter Property="Width" Value="28" /><Setter Property="Height" Value="28" /><Setter Property="FontSize" Value="11" /></Style>
        <Style Selector="^[Size=Lg]"><Setter Property="Width" Value="48" /><Setter Property="Height" Value="48" /><Setter Property="FontSize" Value="16" /></Style>
        <Style Selector="^:online /template/ Ellipse#Status"><Setter Property="IsVisible" Value="True" /><Setter Property="Fill" Value="{{DynamicResource Helix.Success}}" /></Style>
        <Style Selector="^:away /template/ Ellipse#Status"><Setter Property="IsVisible" Value="True" /><Setter Property="Fill" Value="{{DynamicResource Helix.Warning}}" /></Style>
        <Style Selector="^:busy /template/ Ellipse#Status"><Setter Property="IsVisible" Value="True" /><Setter Property="Fill" Value="{{DynamicResource Helix.Danger}}" /></Style>
        <Style Selector="^:offline /template/ Ellipse#Status"><Setter Property="IsVisible" Value="True" /><Setter Property="Fill" Value="{{DynamicResource Helix.TextSubtle}}" /></Style>
    </ControlTheme>

    <ControlTheme x:Key="{{x:Type h:HelixAvatarGroup}}" TargetType="h:HelixAvatarGroup">
        <Setter Property="ItemsPanel"><ItemsPanelTemplate><StackPanel Orientation="Horizontal" /></ItemsPanelTemplate></Setter>
        <Setter Property="Template">
            <ControlTemplate>
                <StackPanel Orientation="Horizontal">
                    <ItemsPresenter Name="PART_ItemsPresenter" ItemsPanel="{{TemplateBinding ItemsPanel}}" />
                    <Border Height="36" MinWidth="36" CornerRadius="999" BorderThickness="2" BorderBrush="{{DynamicResource Helix.Surface}}" Background="{{DynamicResource Helix.Surface3}}"
                            Margin="-10,0,0,0" Padding="6,0" IsVisible="{{TemplateBinding OverflowText, Converter={{x:Static StringConverters.IsNotNullOrEmpty}}}}">
                        <TextBlock Text="{{TemplateBinding OverflowText}}" FontSize="12.5" FontWeight="SemiBold" Foreground="{{DynamicResource Helix.TextMuted}}" HorizontalAlignment="Center" VerticalAlignment="Center" />
                    </Border>
                </StackPanel>
            </ControlTemplate>
        </Setter>
    </ControlTheme>

    <ControlTheme x:Key="{{x:Type h:HelixSkeleton}}" TargetType="h:HelixSkeleton">
        <Setter Property="Background" Value="{{DynamicResource Helix.Surface3}}" />
        <Setter Property="CornerRadius" Value="6" />
        <Setter Property="Template">
            <ControlTemplate>
                <Panel>
                    <ItemsControl Name="Lines" ItemsSource="{{TemplateBinding LineWidths}}">
                        <ItemsControl.ItemsPanel><ItemsPanelTemplate><StackPanel Spacing="9" /></ItemsPanelTemplate></ItemsControl.ItemsPanel>
                        <ItemsControl.ItemTemplate>
                            <DataTemplate x:DataType="x:Double">
                                <Border Height="12" CornerRadius="6" Background="{{DynamicResource Helix.Surface3}}" HorizontalAlignment="Left">
                                    <Border.Width>
                                        <MultiBinding Converter="{{x:Static h:HelixConverters.Share}}">
                                            <Binding Path="$parent[ItemsControl].Bounds.Width" />
                                            <Binding />
                                        </MultiBinding>
                                    </Border.Width>
                                </Border>
                            </DataTemplate>
                        </ItemsControl.ItemTemplate>
                    </ItemsControl>
                    <Border Name="Block" Background="{{TemplateBinding Background}}" CornerRadius="{{TemplateBinding CornerRadius}}" IsVisible="False" />
                </Panel>
            </ControlTemplate>
        </Setter>
        <Style Selector="^[IsCircle=True] /template/ Border#Block"><Setter Property="IsVisible" Value="True" /><Setter Property="CornerRadius" Value="999" /></Style>
        <Style Selector="^[IsCircle=True] /template/ ItemsControl#Lines"><Setter Property="IsVisible" Value="False" /></Style>
        <Style Selector="^">
            <Style.Animations>
                <Animation Duration="0:0:1.4" IterationCount="Infinite" PlaybackDirection="Alternate">
                    <KeyFrame Cue="0%"><Setter Property="Opacity" Value="1" /></KeyFrame>
                    <KeyFrame Cue="100%"><Setter Property="Opacity" Value="0.45" /></KeyFrame>
                </Animation>
            </Style.Animations>
        </Style>
    </ControlTheme>

    <ControlTheme x:Key="{{x:Type h:HelixSplitter}}" TargetType="h:HelixSplitter">
        <Setter Property="Background" Value="Transparent" />
        <Setter Property="MinWidth" Value="8" />
        <Setter Property="MinHeight" Value="8" />
        <Setter Property="Focusable" Value="True" />
        <Setter Property="Template">
            <ControlTemplate>
                <Panel Background="{{TemplateBinding Background}}">
                    <Border Name="Line" Background="{{DynamicResource Helix.Border}}" Width="1" />
                    <Border Name="Grip" Width="4" Height="28" CornerRadius="2" Background="{{DynamicResource Helix.BorderStrong}}" />
                </Panel>
            </ControlTemplate>
        </Setter>
        <Style Selector="^:pointerover /template/ Border#Line"><Setter Property="Background" Value="{{DynamicResource Helix.Primary}}" /><Setter Property="Width" Value="2" /></Style>
        <Style Selector="^:pointerover /template/ Border#Grip"><Setter Property="Background" Value="{{DynamicResource Helix.Primary}}" /></Style>
        <Style Selector="^[ResizeDirection=Rows] /template/ Border#Line"><Setter Property="Width" Value="NaN" /><Setter Property="Height" Value="1" /></Style>
        <Style Selector="^[ResizeDirection=Rows] /template/ Border#Grip"><Setter Property="Width" Value="28" /><Setter Property="Height" Value="4" /></Style>
    </ControlTheme>

    <!-- ===================================================================== Menus -->

    <ControlTheme x:Key="{{x:Type h:HelixContextMenu}}" TargetType="h:HelixContextMenu">
        <Setter Property="Background" Value="{{DynamicResource Helix.Surface}}" />
        <Setter Property="BorderBrush" Value="{{DynamicResource Helix.Border}}" />
        <Setter Property="BorderThickness" Value="1" />
        <Setter Property="Padding" Value="5" />
        <Setter Property="MinWidth" Value="220" />
        <Setter Property="FontFamily" Value="{{DynamicResource Helix.FontFamily}}" />
        <Setter Property="FontSize" Value="{{DynamicResource Helix.FontSize}}" />
        <Setter Property="ItemsPanel"><ItemsPanelTemplate><StackPanel /></ItemsPanelTemplate></Setter>
        <Setter Property="Template">
            <ControlTemplate>
                <Border Background="{{TemplateBinding Background}}" BorderBrush="{{TemplateBinding BorderBrush}}" BorderThickness="{{TemplateBinding BorderThickness}}"
                        CornerRadius="12" Padding="{{TemplateBinding Padding}}" BoxShadow="0 12 32 0 #26000000" Margin="0,0,12,12">
                    <ItemsPresenter Name="PART_ItemsPresenter" ItemsPanel="{{TemplateBinding ItemsPanel}}" KeyboardNavigation.TabNavigation="Continue" />
                </Border>
            </ControlTemplate>
        </Setter>
    </ControlTheme>

    <ControlTheme x:Key="{{x:Type h:HelixMenuItem}}" TargetType="h:HelixMenuItem">
        <Setter Property="Foreground" Value="{{DynamicResource Helix.Text}}" />
        <Setter Property="Background" Value="Transparent" />
        <Setter Property="Cursor" Value="Hand" />
        <Setter Property="Template">
            <ControlTemplate>
                <Border Name="Bg" Background="{{TemplateBinding Background}}" CornerRadius="7" Padding="10,7">
                    <DockPanel>
                        <TextBlock DockPanel.Dock="Right" Text="{{TemplateBinding Shortcut}}" FontSize="11.5" Foreground="{{DynamicResource Helix.TextSubtle}}" VerticalAlignment="Center" Margin="16,0,0,0" />
                        <h:HelixIcon Name="Icon" DockPanel.Dock="Left" Icon="{{TemplateBinding IconName}}" Size="15" Foreground="{{DynamicResource Helix.TextSubtle}}" VerticalAlignment="Top"
                                     Margin="0,2,10,0" IsVisible="{{TemplateBinding IconName, Converter={{x:Static ObjectConverters.IsNotNull}}}}" />
                        <StackPanel Spacing="1">
                            <ContentPresenter Name="PART_HeaderPresenter" Content="{{TemplateBinding Header}}" />
                            <TextBlock Text="{{TemplateBinding Description}}" FontSize="12" Foreground="{{DynamicResource Helix.TextMuted}}" TextWrapping="Wrap"
                                       IsVisible="{{TemplateBinding Description, Converter={{x:Static StringConverters.IsNotNullOrEmpty}}}}" />
                        </StackPanel>
                    </DockPanel>
                </Border>
            </ControlTemplate>
        </Setter>
        <Style Selector="^:pointerover"><Setter Property="Background" Value="{{DynamicResource Helix.Surface2}}" /></Style>
        <Style Selector="^:selected"><Setter Property="Background" Value="{{DynamicResource Helix.Surface2}}" /></Style>
        <Style Selector="^[IsDanger=True]"><Setter Property="Foreground" Value="{{DynamicResource Helix.Danger}}" /></Style>
        <Style Selector="^[IsDanger=True] /template/ h|HelixIcon#Icon"><Setter Property="Foreground" Value="{{DynamicResource Helix.Danger}}" /></Style>
        <Style Selector="^:disabled"><Setter Property="Opacity" Value="0.45" /></Style>
    </ControlTheme>

    <ControlTheme x:Key="{{x:Type h:HelixMenuSeparator}}" TargetType="h:HelixMenuSeparator">
        <Setter Property="Focusable" Value="False" />
        <Setter Property="Template"><ControlTemplate><Border Height="1" Margin="6,5" Background="{{DynamicResource Helix.Border}}" /></ControlTemplate></Setter>
    </ControlTheme>

    <ControlTheme x:Key="{{x:Type h:HelixMenuLabel}}" TargetType="h:HelixMenuLabel">
        <Setter Property="Template">
            <ControlTemplate>
                <TextBlock Text="{{TemplateBinding Text}}" FontSize="11.5" FontWeight="SemiBold" Foreground="{{DynamicResource Helix.TextSubtle}}" Margin="10,6,10,4" />
            </ControlTemplate>
        </Setter>
    </ControlTheme>
'''
