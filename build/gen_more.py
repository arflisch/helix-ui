"""Templates of the tree view, carousel and log viewer (appended to Controls.axaml)."""

LIST_ITEM = '''<StackPanel>
                    <Border Name="Row" CornerRadius="8" Padding="4,5" Margin="0,1" Background="Transparent">
                        <DockPanel Margin="{TemplateBinding Indent}">
                            <ToggleButton Name="Expander" DockPanel.Dock="Left" Theme="{StaticResource HelixTreeToggle}" Margin="0,0,4,0" VerticalAlignment="Top"
                                          IsChecked="{TemplateBinding IsExpanded, Mode=TwoWay}" />
                            <ContentPresenter Name="PART_HeaderPresenter" Content="{TemplateBinding Header}" ContentTemplate="{TemplateBinding HeaderTemplate}" VerticalAlignment="Center" />
                        </DockPanel>
                    </Border>
                    <ItemsPresenter Name="PART_ItemsPresenter" ItemsPanel="{TemplateBinding ItemsPanel}" IsVisible="{TemplateBinding IsExpanded}" />
                </StackPanel>'''

DIAGRAM_ITEM = '''<StackPanel HorizontalAlignment="Center" VerticalAlignment="Top">
                    <Grid Name="TopConnector" Height="18" ColumnDefinitions="*,*">
                        <Border Name="LeftLine" BorderBrush="{DynamicResource Helix.BorderStrong}" BorderThickness="0,1.5,0,0" />
                        <Border Name="RightLine" Grid.Column="1" BorderBrush="{DynamicResource Helix.BorderStrong}" BorderThickness="0,1.5,0,0" />
                        <Border Grid.ColumnSpan="2" Width="1.5" HorizontalAlignment="Center" Background="{DynamicResource Helix.BorderStrong}" />
                    </Grid>
                    <Border Name="Node" HorizontalAlignment="Center" Margin="10,0" MinWidth="150" Padding="14,10" CornerRadius="12" Cursor="Hand"
                            Background="{DynamicResource Helix.Surface}" BorderBrush="{DynamicResource Helix.Border}" BorderThickness="1" BoxShadow="0 1 3 0 #14000000">
                        <ContentPresenter Name="PART_HeaderPresenter" Content="{TemplateBinding Header}" ContentTemplate="{TemplateBinding HeaderTemplate}" HorizontalAlignment="Center" />
                    </Border>
                    <ToggleButton Name="Expander" Theme="{StaticResource HelixTreeSign}" Margin="0,-10,0,0" HorizontalAlignment="Center"
                                  IsChecked="{TemplateBinding IsExpanded, Mode=TwoWay}" />
                    <StackPanel Name="Below" IsVisible="{TemplateBinding ShowsChildren}">
                        <Border Width="1.5" Height="12" Background="{DynamicResource Helix.BorderStrong}" HorizontalAlignment="Center" />
                        <ItemsPresenter Name="PART_ItemsPresenter" ItemsPanel="{TemplateBinding ItemsPanel}" />
                    </StackPanel>
                </StackPanel>'''

def build():
    return '''
    <!-- ===================================================================== Tree view -->

    <ControlTheme x:Key="HelixTreeToggle" TargetType="ToggleButton">
        <Setter Property="Width" Value="20" />
        <Setter Property="Height" Value="20" />
        <Setter Property="Background" Value="Transparent" />
        <Setter Property="Cursor" Value="Hand" />
        <Setter Property="Focusable" Value="False" />
        <Setter Property="Template">
            <ControlTemplate>
                <Border Background="{TemplateBinding Background}" CornerRadius="5">
                    <h:HelixIcon Name="Chevron" Icon="chevron-right" Size="14" Foreground="{DynamicResource Helix.TextMuted}" HorizontalAlignment="Center" VerticalAlignment="Center">
                        <h:HelixIcon.Transitions><Transitions><TransformOperationsTransition Property="RenderTransform" Duration="0:0:0.15" /></Transitions></h:HelixIcon.Transitions>
                    </h:HelixIcon>
                </Border>
            </ControlTemplate>
        </Setter>
        <Style Selector="^:pointerover"><Setter Property="Background" Value="{DynamicResource Helix.Surface3}" /></Style>
        <Style Selector="^:checked /template/ h|HelixIcon#Chevron"><Setter Property="RenderTransform" Value="rotate(90deg)" /></Style>
    </ControlTheme>

    <ControlTheme x:Key="HelixTreeSign" TargetType="ToggleButton">
        <Setter Property="Width" Value="20" />
        <Setter Property="Height" Value="20" />
        <Setter Property="Cursor" Value="Hand" />
        <Setter Property="Focusable" Value="False" />
        <Setter Property="Template">
            <ControlTemplate>
                <Border Background="{DynamicResource Helix.Surface}" BorderBrush="{DynamicResource Helix.BorderStrong}" BorderThickness="1" CornerRadius="10">
                    <h:HelixIcon Name="Sign" Icon="plus" Size="11" StrokeWidth="2.6" Foreground="{DynamicResource Helix.TextMuted}" HorizontalAlignment="Center" VerticalAlignment="Center" />
                </Border>
            </ControlTemplate>
        </Setter>
        <Style Selector="^:checked /template/ h|HelixIcon#Sign"><Setter Property="Icon" Value="minus" /></Style>
    </ControlTheme>

    <ControlTheme x:Key="{x:Type h:HelixTreeViewItem}" TargetType="h:HelixTreeViewItem">
        <Setter Property="Foreground" Value="{DynamicResource Helix.Text}" />
        <Setter Property="FontFamily" Value="{DynamicResource Helix.FontFamily}" />
        <Setter Property="FontSize" Value="{DynamicResource Helix.FontSize}" />
        <Setter Property="Template"><ControlTemplate>''' + LIST_ITEM + '''</ControlTemplate></Setter>
        <Style Selector="^:leaf /template/ ToggleButton#Expander"><Setter Property="Opacity" Value="0" /><Setter Property="IsHitTestVisible" Value="False" /></Style>
        <Style Selector="^ /template/ Border#Row:pointerover"><Setter Property="Background" Value="{DynamicResource Helix.Surface2}" /></Style>
        <Style Selector="^:selected /template/ Border#Row"><Setter Property="Background" Value="{DynamicResource Helix.PrimarySoft}" /></Style>
        <Style Selector="^:selected /template/ ContentPresenter#PART_HeaderPresenter"><Setter Property="FontWeight" Value="SemiBold" /></Style>
        <Style Selector="^:disabled"><Setter Property="Opacity" Value="0.5" /></Style>

        <Style Selector="^[Layout=Diagram]">
            <Setter Property="Template"><ControlTemplate>''' + DIAGRAM_ITEM + '''</ControlTemplate></Setter>
            <Setter Property="ItemsPanel"><ItemsPanelTemplate><StackPanel Orientation="Horizontal" HorizontalAlignment="Center" /></ItemsPanelTemplate></Setter>
        </Style>
        <Style Selector="^[Layout=Diagram]:root-level /template/ Grid#TopConnector"><Setter Property="IsVisible" Value="False" /></Style>
        <Style Selector="^[Layout=Diagram]:nth-child(1) /template/ Border#LeftLine"><Setter Property="BorderThickness" Value="0" /></Style>
        <Style Selector="^[Layout=Diagram]:nth-last-child(1) /template/ Border#RightLine"><Setter Property="BorderThickness" Value="0" /></Style>
        <Style Selector="^[Layout=Diagram]:leaf /template/ ToggleButton#Expander"><Setter Property="IsVisible" Value="False" /></Style>
        <Style Selector="^[Layout=Diagram] /template/ Border#Node:pointerover"><Setter Property="BorderBrush" Value="{DynamicResource Helix.BorderStrong}" /></Style>
        <Style Selector="^[Layout=Diagram]:selected /template/ Border#Node">
            <Setter Property="BorderBrush" Value="{DynamicResource Helix.Primary}" />
            <Setter Property="BorderThickness" Value="2" />
        </Style>
        <Style Selector="^[Layout=Diagram]:selected /template/ ContentPresenter#PART_HeaderPresenter"><Setter Property="FontWeight" Value="Normal" /></Style>
    </ControlTheme>

    <ControlTheme x:Key="{x:Type h:HelixTreeView}" TargetType="h:HelixTreeView">
        <Setter Property="FontFamily" Value="{DynamicResource Helix.FontFamily}" />
        <Setter Property="FontSize" Value="{DynamicResource Helix.FontSize}" />
        <Setter Property="Foreground" Value="{DynamicResource Helix.Text}" />
        <Setter Property="Background" Value="Transparent" />
        <Setter Property="Template">
            <ControlTemplate>
                <ScrollViewer Name="Scroll" Background="{TemplateBinding Background}" HorizontalScrollBarVisibility="Disabled" VerticalScrollBarVisibility="Auto">
                    <ItemsPresenter Name="PART_ItemsPresenter" ItemsPanel="{TemplateBinding ItemsPanel}" />
                </ScrollViewer>
            </ControlTemplate>
        </Setter>
        <Style Selector="^[Layout=Diagram]">
            <Setter Property="ItemsPanel"><ItemsPanelTemplate><StackPanel Orientation="Horizontal" HorizontalAlignment="Center" /></ItemsPanelTemplate></Setter>
        </Style>
        <Style Selector="^[Layout=Diagram] /template/ ScrollViewer#Scroll"><Setter Property="HorizontalScrollBarVisibility" Value="Auto" /></Style>
    </ControlTheme>

    <!-- ===================================================================== Carousel -->

    <ControlTheme x:Key="HelixCarouselArrow" TargetType="Button">
        <Setter Property="Width" Value="36" />
        <Setter Property="Height" Value="36" />
        <Setter Property="Cursor" Value="Hand" />
        <Setter Property="Foreground" Value="{DynamicResource Helix.Text}" />
        <Setter Property="Background" Value="{DynamicResource Helix.Surface}" />
        <Setter Property="Template">
            <ControlTemplate>
                <Border CornerRadius="18" Background="{TemplateBinding Background}" BorderBrush="{DynamicResource Helix.Border}" BorderThickness="1" BoxShadow="0 2 6 0 #1F000000">
                    <ContentPresenter Content="{TemplateBinding Content}" HorizontalAlignment="Center" VerticalAlignment="Center" />
                </Border>
            </ControlTemplate>
        </Setter>
        <Style Selector="^:pointerover"><Setter Property="Background" Value="{DynamicResource Helix.Surface2}" /></Style>
        <Style Selector="^:disabled"><Setter Property="Opacity" Value="0.35" /></Style>
    </ControlTheme>

    <ControlTheme x:Key="{x:Type h:HelixCarousel}" TargetType="h:HelixCarousel">
        <Setter Property="ItemsPanel"><ItemsPanelTemplate><h:HelixCarouselPanel /></ItemsPanelTemplate></Setter>
        <Setter Property="Template">
            <ControlTemplate>
                <StackPanel>
                    <Panel>
                        <ItemsPresenter Name="PART_ItemsPresenter" ItemsPanel="{TemplateBinding ItemsPanel}" />
                        <Button Name="PART_Prev" Theme="{StaticResource HelixCarouselArrow}" HorizontalAlignment="Left" VerticalAlignment="Center" Margin="10,0"
                                IsVisible="{TemplateBinding ShowArrows}"><h:HelixIcon Icon="chevron-left" /></Button>
                        <Button Name="PART_Next" Theme="{StaticResource HelixCarouselArrow}" HorizontalAlignment="Right" VerticalAlignment="Center" Margin="10,0"
                                IsVisible="{TemplateBinding ShowArrows}"><h:HelixIcon Icon="chevron-right" /></Button>
                    </Panel>
                    <StackPanel Orientation="Horizontal" HorizontalAlignment="Center" Margin="0,12,0,0">
                        <Button Name="PART_Play" Theme="{StaticResource HelixIconButton}" Margin="0,0,8,0" IsVisible="False" />
                        <StackPanel Name="PART_Dots" Orientation="Horizontal" VerticalAlignment="Center" />
                    </StackPanel>
                </StackPanel>
            </ControlTemplate>
        </Setter>
    </ControlTheme>

    <!-- ===================================================================== Log viewer -->

    <ControlTheme x:Key="HelixLogLevelToggle" TargetType="ToggleButton">
        <Setter Property="Background" Value="Transparent" />
        <Setter Property="Foreground" Value="{DynamicResource Helix.TextSubtle}" />
        <Setter Property="FontSize" Value="12.5" />
        <Setter Property="FontWeight" Value="Medium" />
        <Setter Property="Cursor" Value="Hand" />
        <Setter Property="Margin" Value="0,0,4,0" />
        <Setter Property="Template">
            <ControlTemplate>
                <Border Background="{TemplateBinding Background}" BorderBrush="{DynamicResource Helix.Border}" BorderThickness="1" CornerRadius="999" Padding="10,4">
                    <ContentPresenter Content="{TemplateBinding Content}" VerticalAlignment="Center" />
                </Border>
            </ControlTemplate>
        </Setter>
        <Style Selector="^:pointerover"><Setter Property="Background" Value="{DynamicResource Helix.Surface2}" /></Style>
        <Style Selector="^:checked">
            <Setter Property="Background" Value="{DynamicResource Helix.PrimarySoft}" />
            <Setter Property="Foreground" Value="{DynamicResource Helix.Primary}" />
        </Style>
    </ControlTheme>

    <ControlTheme x:Key="HelixLogLine" TargetType="ListBoxItem">
        <Setter Property="Background" Value="Transparent" />
        <Setter Property="Template">
            <ControlTemplate>
                <Border Background="{TemplateBinding Background}" Height="30" Padding="12,0" BorderBrush="{DynamicResource Helix.Border}" BorderThickness="0,0,0,1">
                    <ContentPresenter Name="PART_ContentPresenter" Content="{TemplateBinding Content}" ContentTemplate="{TemplateBinding ContentTemplate}" VerticalAlignment="Center" />
                </Border>
            </ControlTemplate>
        </Setter>
        <Style Selector="^:pointerover"><Setter Property="Background" Value="{DynamicResource Helix.Surface2}" /></Style>
        <Style Selector="^:selected"><Setter Property="Background" Value="{DynamicResource Helix.PrimarySoft}" /></Style>
    </ControlTheme>

    <ControlTheme x:Key="{x:Type h:HelixLogViewer}" TargetType="h:HelixLogViewer">
        <Setter Property="Height" Value="360" />
        <Setter Property="FontFamily" Value="{DynamicResource Helix.FontFamily}" />
        <Setter Property="Template">
            <ControlTemplate>
                <Border Background="{DynamicResource Helix.Surface}" BorderBrush="{DynamicResource Helix.Border}" BorderThickness="1" CornerRadius="14" ClipToBounds="True">
                    <DockPanel>
                        <Border DockPanel.Dock="Top" Padding="12,10" BorderBrush="{DynamicResource Helix.Border}" BorderThickness="0,0,0,1">
                            <StackPanel Spacing="8">
                                <DockPanel>
                                    <h:HelixSwitch DockPanel.Dock="Right" Content="{x:Static h:HelixLogViewer.FollowText}" VerticalAlignment="Center" Margin="10,0,0,0"
                                                   IsChecked="{Binding IsFollowing, RelativeSource={RelativeSource TemplatedParent}, Mode=TwoWay}" />
                                    <StackPanel DockPanel.Dock="Left" Orientation="Horizontal" Margin="0,0,10,0" VerticalAlignment="Center">
                                        <ToggleButton Theme="{StaticResource HelixLogLevelToggle}" Content="{TemplateBinding DebugLabel}"
                                                      IsChecked="{Binding ShowDebug, RelativeSource={RelativeSource TemplatedParent}, Mode=TwoWay}" />
                                        <ToggleButton Theme="{StaticResource HelixLogLevelToggle}" Content="{TemplateBinding InfoLabel}"
                                                      IsChecked="{Binding ShowInfo, RelativeSource={RelativeSource TemplatedParent}, Mode=TwoWay}" />
                                        <ToggleButton Theme="{StaticResource HelixLogLevelToggle}" Content="{TemplateBinding WarningLabel}"
                                                      IsChecked="{Binding ShowWarning, RelativeSource={RelativeSource TemplatedParent}, Mode=TwoWay}" />
                                        <ToggleButton Theme="{StaticResource HelixLogLevelToggle}" Content="{TemplateBinding ErrorLabel}"
                                                      IsChecked="{Binding ShowError, RelativeSource={RelativeSource TemplatedParent}, Mode=TwoWay}" />
                                    </StackPanel>
                                    <h:HelixTextField Icon="search" IsClearable="True" Placeholder="{x:Static h:HelixLogViewer.SearchPlaceholderText}" MaxWidth="280" HorizontalAlignment="Left"
                                                      Text="{Binding SearchText, RelativeSource={RelativeSource TemplatedParent}, Mode=TwoWay}" />
                                </DockPanel>
                                <DockPanel>
                                    <StackPanel DockPanel.Dock="Right" Orientation="Horizontal" Spacing="4" IsVisible="{TemplateBinding IsExportable}">
                                        <h:HelixButton Content="CSV" Icon="download" Variant="Ghost" Tone="Neutral" Size="Sm" Command="{Binding ExportCsvCommand, RelativeSource={RelativeSource TemplatedParent}}" />
                                        <h:HelixButton Content="Excel" Icon="download" Variant="Ghost" Tone="Neutral" Size="Sm" Command="{Binding ExportExcelCommand, RelativeSource={RelativeSource TemplatedParent}}" />
                                    </StackPanel>
                                    <StackPanel Orientation="Horizontal" Spacing="8">
                                        <h:HelixSelect Width="190" ItemsSource="{TemplateBinding Sources}" IsVisible="{TemplateBinding HasSources}"
                                                       SelectedItem="{Binding SourceChoice, RelativeSource={RelativeSource TemplatedParent}, Mode=TwoWay}" />
                                        <h:HelixSelect Width="190" ItemsSource="{Binding Periods, RelativeSource={RelativeSource TemplatedParent}}"
                                                       SelectedItem="{Binding Period, RelativeSource={RelativeSource TemplatedParent}, Mode=TwoWay}" />
                                    </StackPanel>
                                </DockPanel>
                            </StackPanel>
                        </Border>
                        <Border DockPanel.Dock="Top" Padding="12,8" BorderBrush="{DynamicResource Helix.Border}" BorderThickness="0,0,0,1" IsVisible="{TemplateBinding HasFilters}">
                            <WrapPanel ItemSpacing="6" LineSpacing="6">
                                <ItemsControl ItemsSource="{Binding Filters, RelativeSource={RelativeSource TemplatedParent}}">
                                    <ItemsControl.ItemsPanel><ItemsPanelTemplate><WrapPanel ItemSpacing="6" LineSpacing="6" /></ItemsPanelTemplate></ItemsControl.ItemsPanel>
                                    <ItemsControl.ItemTemplate>
                                        <DataTemplate DataType="h:HelixLogFilter">
                                            <Border CornerRadius="999" Padding="10,2,4,2" Background="{DynamicResource Helix.PrimarySoft}" BorderBrush="{DynamicResource Helix.Primary}" BorderThickness="1">
                                                <StackPanel Orientation="Horizontal" Spacing="4">
                                                    <TextBlock Text="{Binding Label}" FontSize="12" FontWeight="Bold" VerticalAlignment="Center" FontFamily="Cascadia Mono, Menlo, Consolas, monospace" />
                                                    <TextBlock Text="=" FontSize="12" VerticalAlignment="Center" />
                                                    <TextBlock Text="{Binding ShortValue}" FontSize="12" VerticalAlignment="Center" FontFamily="Cascadia Mono, Menlo, Consolas, monospace" />
                                                    <Button Theme="{StaticResource HelixIconButton}" Width="20" Height="20" Command="{Binding RemoveCommand}"><h:HelixIcon Icon="x" Size="11" /></Button>
                                                </StackPanel>
                                            </Border>
                                        </DataTemplate>
                                    </ItemsControl.ItemTemplate>
                                </ItemsControl>
                                <Button Theme="{StaticResource HelixTextButton}" Content="{x:Static h:HelixLogViewer.ClearFiltersText}" Command="{Binding ClearFiltersCommand, RelativeSource={RelativeSource TemplatedParent}}"
                                        FontSize="12" Foreground="{DynamicResource Helix.Primary}" VerticalAlignment="Center" />
                            </WrapPanel>
                        </Border>
                        <TextBlock DockPanel.Dock="Bottom" Text="{TemplateBinding StatusText}" FontSize="12" Foreground="{DynamicResource Helix.TextSubtle}" Margin="12,6" />
                        <ContentControl Name="PART_Details" DockPanel.Dock="Bottom" IsVisible="False" Background="{DynamicResource Helix.Surface2}"
                                        BorderBrush="{DynamicResource Helix.Border}" BorderThickness="0,1,0,1" />
                        <Panel>
                            <ListBox Name="PART_Lines" ItemsSource="{TemplateBinding View}" Background="Transparent" BorderThickness="0" ItemContainerTheme="{StaticResource HelixLogLine}"
                                     SelectedItem="{Binding SelectedEntry, RelativeSource={RelativeSource TemplatedParent}, Mode=TwoWay}"
                                     ScrollViewer.HorizontalScrollBarVisibility="Disabled" FontFamily="Cascadia Mono, Menlo, Consolas, monospace" FontSize="12.5">
                                <ListBox.ItemTemplate>
                                    <DataTemplate DataType="h:HelixLogEntry">
                                        <DockPanel>
                                            <TextBlock DockPanel.Dock="Left" Width="72" Foreground="{DynamicResource Helix.TextSubtle}">
                                                <TextBlock.Text>
                                                    <MultiBinding Converter="{x:Static h:HelixLogViewer.TimeConverter}">
                                                        <Binding Path="Time" />
                                                        <Binding Path="TimeFormat" RelativeSource="{RelativeSource AncestorType=h:HelixLogViewer}" />
                                                    </MultiBinding>
                                                </TextBlock.Text>
                                            </TextBlock>
                                            <TextBlock DockPanel.Dock="Left" Width="72" Text="{Binding LevelText}" FontSize="11" FontWeight="Bold" VerticalAlignment="Center"
                                                       Classes.info="{Binding IsInfo}" Classes.warning="{Binding IsWarning}" Classes.error="{Binding IsError}">
                                                <TextBlock.Styles>
                                                    <Style Selector="TextBlock"><Setter Property="Foreground" Value="{DynamicResource Helix.TextSubtle}" /></Style>
                                                    <Style Selector="TextBlock.info"><Setter Property="Foreground" Value="{DynamicResource Helix.Info}" /></Style>
                                                    <Style Selector="TextBlock.warning"><Setter Property="Foreground" Value="{DynamicResource Helix.Warning}" /></Style>
                                                    <Style Selector="TextBlock.error"><Setter Property="Foreground" Value="{DynamicResource Helix.Danger}" /></Style>
                                                </TextBlock.Styles>
                                            </TextBlock>
                                            <h:HelixIcon DockPanel.Dock="Right" Icon="danger" Size="13" Foreground="{DynamicResource Helix.Danger}" VerticalAlignment="Center"
                                                         IsVisible="{Binding HasException}" Margin="8,0,0,0" />
                                            <TextBlock DockPanel.Dock="Left" MaxWidth="150" Margin="0,0,10,0" Text="{Binding Source}" TextTrimming="CharacterEllipsis" Foreground="{DynamicResource Helix.TextMuted}" />
                                            <TextBlock Text="{Binding Message}" TextTrimming="CharacterEllipsis" ToolTip.Tip="{Binding Message}" Foreground="{DynamicResource Helix.Text}" />
                                        </DockPanel>
                                    </DataTemplate>
                                </ListBox.ItemTemplate>
                            </ListBox>
                            <h:HelixButton HorizontalAlignment="Center" VerticalAlignment="Bottom" Margin="0,0,0,12" Size="Sm" Icon="arrow-down"
                                           Content="{TemplateBinding UnseenText}" IsVisible="{TemplateBinding HasUnseen}" Command="{Binding JumpToNewestCommand, RelativeSource={RelativeSource TemplatedParent}}" />
                        </Panel>
                    </DockPanel>
                </Border>
            </ControlTemplate>
        </Setter>
    </ControlTheme>
'''
