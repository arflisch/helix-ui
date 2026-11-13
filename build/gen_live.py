"""Templates of the live controls: time bar (countdown) and notification center (appended to Controls.axaml)."""

def build():
    return '''
    <!-- ===================================================================== Time bar -->

    <ControlTheme x:Key="{x:Type h:HelixTimeBar}" TargetType="h:HelixTimeBar">
        <Setter Property="FontFamily" Value="{DynamicResource Helix.FontFamily}" />
        <Setter Property="Template">
            <ControlTemplate>
                <StackPanel>
                    <DockPanel Margin="0,0,0,6">
                        <TextBlock Name="Count" DockPanel.Dock="Right" Text="{TemplateBinding RemainingText}" FontSize="26" FontWeight="SemiBold"
                                   Foreground="{DynamicResource Helix.Primary}" VerticalAlignment="Bottom" FontFeatures="tnum" />
                        <TextBlock Text="{TemplateBinding Label}" FontSize="13" FontWeight="Medium" Foreground="{DynamicResource Helix.TextMuted}"
                                   VerticalAlignment="Bottom" Margin="0,0,12,4" TextTrimming="CharacterEllipsis" />
                    </DockPanel>
                    <Border Name="PART_Track" Height="{TemplateBinding TrackHeight}" CornerRadius="4" ClipToBounds="True" Background="{DynamicResource Helix.Surface2}">
                        <Border Name="PART_Fill" HorizontalAlignment="Left" CornerRadius="4" Background="{DynamicResource Helix.Primary}">
                            <Border.Transitions><Transitions><DoubleTransition Property="Width" Duration="0:0:0.3" /></Transitions></Border.Transitions>
                        </Border>
                    </Border>
                    <DockPanel Name="Foot" Margin="0,6,0,0">
                        <TextBlock DockPanel.Dock="Right" Text="{TemplateBinding Hint}" FontSize="12.5" Foreground="{DynamicResource Helix.TextSubtle}" Margin="12,0,0,0" />
                        <TextBlock Text="{TemplateBinding UntilText}" FontSize="12.5" Foreground="{DynamicResource Helix.TextMuted}" />
                    </DockPanel>
                    <ItemsControl Name="SlotList" ItemsSource="{TemplateBinding Slots}" IsVisible="{TemplateBinding HasSlots}" Margin="0,8,-6,0">
                        <ItemsControl.ItemsPanel><ItemsPanelTemplate><UniformGrid Rows="1" /></ItemsPanelTemplate></ItemsControl.ItemsPanel>
                        <ItemsControl.ItemTemplate>
                            <DataTemplate DataType="h:HelixTimeBarSlotView">
                                <Border Classes="chip" Classes.current="{Binding IsCurrent}" Classes.past="{Binding IsPast}" Margin="0,0,6,0" Padding="10,7" CornerRadius="6" BorderThickness="1">
                                    <Border.Styles>
                                        <Style Selector="Border.chip"><Setter Property="BorderBrush" Value="{DynamicResource Helix.Border}" /><Setter Property="Background" Value="{DynamicResource Helix.Surface}" /></Style>
                                        <Style Selector="Border.chip.past"><Setter Property="Opacity" Value="0.6" /></Style>
                                        <Style Selector="Border.chip.current"><Setter Property="BorderBrush" Value="{DynamicResource Helix.Primary}" /><Setter Property="Background" Value="{DynamicResource Helix.PrimarySoft}" /></Style>
                                    </Border.Styles>
                                    <StackPanel>
                                        <StackPanel Orientation="Horizontal">
                                            <Ellipse Width="8" Height="8" Margin="0,0,6,0" VerticalAlignment="Center" Fill="{Binding Brush}" IsVisible="{Binding HasBrush}" />
                                            <TextBlock Text="{Binding Label}" FontSize="13" FontWeight="SemiBold" Foreground="{DynamicResource Helix.Text}" />
                                        </StackPanel>
                                        <TextBlock Text="{Binding Text}" FontSize="11.5" MinHeight="15" Foreground="{DynamicResource Helix.TextMuted}" TextTrimming="CharacterEllipsis" />
                                    </StackPanel>
                                </Border>
                            </DataTemplate>
                        </ItemsControl.ItemTemplate>
                    </ItemsControl>
                </StackPanel>
            </ControlTemplate>
        </Setter>
        <Style Selector="^[Compact=True] /template/ DockPanel#Foot"><Setter Property="IsVisible" Value="False" /></Style>
        <Style Selector="^[Compact=True] /template/ ItemsControl#SlotList"><Setter Property="IsVisible" Value="False" /></Style>
        <Style Selector="^[Compact=True] /template/ TextBlock#Count"><Setter Property="FontSize" Value="15" /></Style>
        <Style Selector="^:warn /template/ TextBlock#Count"><Setter Property="Foreground" Value="{DynamicResource Helix.Warning}" /></Style>
        <Style Selector="^:warn /template/ Border#PART_Fill"><Setter Property="Background" Value="{DynamicResource Helix.Warning}" /></Style>
        <Style Selector="^:critical /template/ TextBlock#Count"><Setter Property="Foreground" Value="{DynamicResource Helix.Danger}" /></Style>
        <Style Selector="^:critical /template/ Border#PART_Fill"><Setter Property="Background" Value="{DynamicResource Helix.Danger}" /></Style>
        <Style Selector="^:passed /template/ TextBlock#Count"><Setter Property="Foreground" Value="{DynamicResource Helix.TextSubtle}" /><Setter Property="FontSize" Value="15" /></Style>
        <Style Selector="^:passed /template/ Border#PART_Fill"><Setter Property="Background" Value="{DynamicResource Helix.TextSubtle}" /></Style>
    </ControlTheme>

    <!-- ===================================================================== Notification center -->

    <ControlTheme x:Key="HelixTextButton" TargetType="Button">
        <Setter Property="Background" Value="Transparent" />
        <Setter Property="Padding" Value="0" />
        <Setter Property="Cursor" Value="Hand" />
        <Setter Property="Template">
            <ControlTemplate>
                <ContentPresenter Content="{TemplateBinding Content}" Background="{TemplateBinding Background}" Padding="{TemplateBinding Padding}" />
            </ControlTemplate>
        </Setter>
        <Style Selector="^:pointerover"><Setter Property="Opacity" Value="0.8" /></Style>
    </ControlTheme>

    <ControlTheme x:Key="HelixNotificationRow" TargetType="Button">
        <Setter Property="Background" Value="Transparent" />
        <Setter Property="HorizontalAlignment" Value="Stretch" />
        <Setter Property="HorizontalContentAlignment" Value="Stretch" />
        <Setter Property="Cursor" Value="Hand" />
        <Setter Property="Template">
            <ControlTemplate>
                <Border Background="{TemplateBinding Background}" CornerRadius="7" Padding="8,9">
                    <ContentPresenter Content="{TemplateBinding Content}" HorizontalContentAlignment="Stretch" />
                </Border>
            </ControlTemplate>
        </Setter>
        <Style Selector="^.unread"><Setter Property="Background" Value="{DynamicResource Helix.PrimarySoft}" /></Style>
        <Style Selector="^:pointerover"><Setter Property="Background" Value="{DynamicResource Helix.Surface2}" /></Style>
    </ControlTheme>

    <ControlTheme x:Key="{x:Type h:HelixNotificationCenter}" TargetType="h:HelixNotificationCenter">
        <Setter Property="FontFamily" Value="{DynamicResource Helix.FontFamily}" />
        <Setter Property="Template">
            <ControlTemplate>
                <Panel>
                    <ToggleButton Name="Bell" Theme="{StaticResource HelixBareToggle}" Width="36" Height="36" CornerRadius="8"
                                  IsChecked="{Binding IsOpen, RelativeSource={RelativeSource TemplatedParent}, Mode=TwoWay}"
                                  AutomationProperties.Name="{x:Static h:HelixNotificationCenter.NotificationsText}">
                        <Panel HorizontalAlignment="Center" VerticalAlignment="Center">
                            <h:HelixIcon Icon="bell" Size="20" Foreground="{DynamicResource Helix.TextMuted}" />
                            <Border HorizontalAlignment="Right" VerticalAlignment="Top" Margin="0,-7,-9,0" MinWidth="17" Height="17" Padding="4,0"
                                    CornerRadius="9" Background="{DynamicResource Helix.Danger}" BorderBrush="{DynamicResource Helix.Surface}" BorderThickness="2"
                                    IsVisible="{TemplateBinding HasUnread}">
                                <TextBlock Text="{TemplateBinding UnreadText}" FontSize="10" FontWeight="Bold" Foreground="White" HorizontalAlignment="Center" VerticalAlignment="Center" />
                            </Border>
                        </Panel>
                    </ToggleButton>
                    <Popup Name="PART_Popup" PlacementTarget="{Binding #Bell}" IsLightDismissEnabled="True" OverlayDismissEventPassThrough="False"
                           IsOpen="{Binding IsOpen, RelativeSource={RelativeSource TemplatedParent}, Mode=TwoWay}">
                        <Border Margin="12,0,12,24" Width="{TemplateBinding PanelWidth}" Background="{DynamicResource Helix.Surface}" BorderBrush="{DynamicResource Helix.Border}"
                                BorderThickness="1" CornerRadius="10" BoxShadow="0 12 32 0 #26000000"
                                TextElement.FontFamily="{DynamicResource Helix.FontFamily}">
                            <DockPanel>
                                <DockPanel DockPanel.Dock="Top" Margin="14,12">
                                    <Button DockPanel.Dock="Right" Theme="{StaticResource HelixTextButton}" Content="{x:Static h:HelixNotificationCenter.MarkAllReadText}"
                                            Command="{Binding MarkAllReadCommand, RelativeSource={RelativeSource TemplatedParent}}" IsVisible="{TemplateBinding HasUnread}"
                                            FontSize="12.5" FontWeight="SemiBold" Foreground="{DynamicResource Helix.Primary}" />
                                    <TextBlock Text="{TemplateBinding PanelTitle}" FontSize="14" FontWeight="SemiBold" Foreground="{DynamicResource Helix.Text}" />
                                </DockPanel>
                                <Border DockPanel.Dock="Top" Height="1" Background="{DynamicResource Helix.Border}" />
                                <DockPanel DockPanel.Dock="Bottom" IsVisible="{TemplateBinding HasItems}">
                                    <Border DockPanel.Dock="Top" Height="1" Background="{DynamicResource Helix.Border}" />
                                    <Button Theme="{StaticResource HelixTextButton}" Content="{x:Static h:HelixNotificationCenter.ClearAllText}"
                                            Command="{Binding ClearCommand, RelativeSource={RelativeSource TemplatedParent}}"
                                            HorizontalAlignment="Left" Margin="14,8" FontSize="12.5" Foreground="{DynamicResource Helix.TextMuted}" />
                                </DockPanel>
                                <Panel>
                                    <StackPanel Margin="16,36" HorizontalAlignment="Center" IsVisible="{Binding !HasItems, RelativeSource={RelativeSource TemplatedParent}}">
                                        <h:HelixIcon Icon="bell" Size="22" Foreground="{DynamicResource Helix.TextMuted}" HorizontalAlignment="Center" Margin="0,0,0,8" />
                                        <TextBlock Text="{x:Static h:HelixNotificationCenter.EmptyText}" FontSize="13" Foreground="{DynamicResource Helix.TextMuted}" />
                                    </StackPanel>
                                    <ScrollViewer MaxHeight="420" IsVisible="{TemplateBinding HasItems}" HorizontalScrollBarVisibility="Disabled">
                                        <ItemsControl ItemsSource="{TemplateBinding Rows}" Margin="6,2,6,6">
                                            <ItemsControl.Styles>
                                                <Style Selector="Button.row Button#Dismiss"><Setter Property="Opacity" Value="0" /></Style>
                                                <Style Selector="Button.row:pointerover Button#Dismiss"><Setter Property="Opacity" Value="1" /></Style>
                                            </ItemsControl.Styles>
                                            <ItemsControl.DataTemplates>
                                                <DataTemplate DataType="h:HelixNotificationGroup">
                                                    <TextBlock Text="{Binding Name}" Margin="8,10,8,4" FontSize="11.5" FontWeight="SemiBold" Foreground="{DynamicResource Helix.TextSubtle}" />
                                                </DataTemplate>
                                                <DataTemplate DataType="h:HelixNotification">
                                                    <Button Theme="{StaticResource HelixNotificationRow}" Classes="row" Command="{Binding OpenCommand}" Classes.unread="{Binding !IsRead}">
                                                        <DockPanel>
                                                            <Ellipse Classes="dot" Classes.read="{Binding IsRead}" Classes.success="{Binding Severity, Converter={x:Static h:HelixConverters.IsSuccess}}"
                                                                     Classes.warning="{Binding Severity, Converter={x:Static h:HelixConverters.IsWarning}}"
                                                                     Classes.danger="{Binding Severity, Converter={x:Static h:HelixConverters.IsDanger}}"
                                                                     DockPanel.Dock="Left" Width="8" Height="8" Margin="0,6,10,0" VerticalAlignment="Top">
                                                                <Ellipse.Styles>
                                                                    <Style Selector="Ellipse.dot"><Setter Property="Fill" Value="{DynamicResource Helix.Info}" /></Style>
                                                                    <Style Selector="Ellipse.dot.success"><Setter Property="Fill" Value="{DynamicResource Helix.Success}" /></Style>
                                                                    <Style Selector="Ellipse.dot.warning"><Setter Property="Fill" Value="{DynamicResource Helix.Warning}" /></Style>
                                                                    <Style Selector="Ellipse.dot.danger"><Setter Property="Fill" Value="{DynamicResource Helix.Danger}" /></Style>
                                                                    <Style Selector="Ellipse.dot.read"><Setter Property="Opacity" Value="0.45" /></Style>
                                                                </Ellipse.Styles>
                                                            </Ellipse>
                                                            <Button Name="Dismiss" DockPanel.Dock="Right" Theme="{StaticResource HelixIconButton}" VerticalAlignment="Top" Margin="6,-2,0,0"
                                                                    Command="{Binding DismissCommand}" ToolTip.Tip="{x:Static h:HelixNotificationCenter.DismissText}">
                                                                <h:HelixIcon Icon="x" Size="14" />
                                                            </Button>
                                                            <StackPanel>
                                                                <TextBlock Text="{Binding Title}" FontSize="13.5" TextWrapping="Wrap" Foreground="{DynamicResource Helix.Text}"
                                                                           Classes.bold="{Binding !IsRead}">
                                                                    <TextBlock.Styles><Style Selector="TextBlock.bold"><Setter Property="FontWeight" Value="SemiBold" /></Style></TextBlock.Styles>
                                                                </TextBlock>
                                                                <TextBlock Text="{Binding Text}" FontSize="12.5" TextWrapping="Wrap" Margin="0,2,0,0" Foreground="{DynamicResource Helix.TextMuted}" IsVisible="{Binding HasText}" />
                                                                <StackPanel Orientation="Horizontal" Margin="0,3,0,0">
                                                                    <TextBlock Text="{Binding TimeText}" FontSize="11.5" Foreground="{DynamicResource Helix.TextSubtle}" />
                                                                    <TextBlock Text="{Binding ActionLine}" FontSize="11.5" FontWeight="SemiBold" Foreground="{DynamicResource Helix.Primary}" IsVisible="{Binding HasAction}" />
                                                                </StackPanel>
                                                            </StackPanel>
                                                        </DockPanel>
                                                    </Button>
                                                </DataTemplate>
                                            </ItemsControl.DataTemplates>
                                        </ItemsControl>
                                    </ScrollViewer>
                                </Panel>
                            </DockPanel>
                        </Border>
                    </Popup>
                </Panel>
            </ControlTemplate>
        </Setter>
    </ControlTheme>
'''
