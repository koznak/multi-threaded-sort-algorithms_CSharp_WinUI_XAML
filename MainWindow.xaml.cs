using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MultiSearchDemoWinUI;

public class VisualizerState
{
    public int[] Data { get; set; }
    public int Active1 { get; set; } = -1;
    public int Active2 { get; set; } = -1;
    public int SortedBegin { get; set; } = 0;
    public int SortedEnd { get; set; } = 100;
    public bool IsFinished { get; set; }
    public int MaxVal { get; set; }
}

public sealed partial class MainWindow : Window
{
    private DispatcherTimer _renderTimer;
    private VisualizerState[] _states;
    private Canvas[] _canvases;
    private Rectangle[][] _rectangles;
    private CancellationTokenSource _cts;

    private readonly SolidColorBrush _barBrush = new SolidColorBrush(ColorHelper.FromArgb(255, 224, 224, 224));
    private readonly SolidColorBrush _greenBrush = new SolidColorBrush(ColorHelper.FromArgb(255, 50, 205, 50));
    private readonly SolidColorBrush _redBrush = new SolidColorBrush(Colors.Red);
    private readonly SolidColorBrush _blueBrush = new SolidColorBrush(Colors.Blue);

    private readonly string[] _algoNames = {
        "Selection Sort", "Shell Sort", "Insertion Sort",
        "Merge Sort", "Quick Sort", "Heap Sort",
        "Bubble Sort", "Comb Sort", "Cocktail Sort"
    };

    public MainWindow()
    {
        this.InitializeComponent();
        CreateUI();
        _renderTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(30) };
        _renderTimer.Tick += OnRenderTimerTick;
        this.Closed += MainWindow_Closed;
    }

    private void MainWindow_Closed(object sender, WindowEventArgs args)
    {
        _renderTimer?.Stop();
        _cts?.Cancel();
    }

    private void CreateUI()
    {
        _states = new VisualizerState[9];
        _canvases = new Canvas[9];
        _rectangles = new Rectangle[9][];

        for (int i = 0; i < 9; i++)
        {
            var panel = new Grid { Background = new SolidColorBrush(ColorHelper.FromArgb(255, 15, 15, 15)) };
            panel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(25) });
            panel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var title = new TextBlock
            {
                Text = _algoNames[i],
                Foreground = new SolidColorBrush(Colors.White),
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 12
            };
            Grid.SetRow(title, 0);
            panel.Children.Add(title);

            var canvas = new Canvas { Background = new SolidColorBrush(Colors.Black) };
            // Ensure bounds can be dynamically calculated later
            Grid.SetRow(canvas, 1);
            panel.Children.Add(canvas);

            Grid.SetColumn(panel, i % 3);
            Grid.SetRow(panel, i / 3);
            MainGrid.Children.Add(panel);

            _canvases[i] = canvas;
            _states[i] = new VisualizerState { Data = Array.Empty<int>() };
        }
    }

    private void StartButton_Click(object sender, RoutedEventArgs e)
    {
        StartButton.IsEnabled = false;
        StopButton.IsEnabled = true;
        _cts?.Cancel();
        _cts = new CancellationTokenSource();

        int numElements = 100;
        var baseData = new int[numElements];
        for (int i = 0; i < numElements; i++) baseData[i] = i + 1;
        
        var rand = new Random();
        for (int i = numElements - 1; i > 0; i--)
        {
            int j = rand.Next(i + 1);
            (baseData[i], baseData[j]) = (baseData[j], baseData[i]);
        }

        for (int i = 0; i < 9; i++)
        {
            _states[i] = new VisualizerState
            {
                Data = (int[])baseData.Clone(),
                MaxVal = numElements,
                SortedBegin = 0,
                SortedEnd = numElements
            };

            _canvases[i].Children.Clear();
            _rectangles[i] = new Rectangle[numElements];
            for (int j = 0; j < numElements; j++)
            {
                var rect = new Rectangle { Fill = _barBrush };
                _canvases[i].Children.Add(rect);
                _rectangles[i][j] = rect;
            }
        }

        _renderTimer.Start();

        Task.Run(() => RunSorts(_cts.Token));
    }

    private void OnRenderTimerTick(object sender, object e)
    {
        bool allFinished = true;
        for (int i = 0; i < 9; i++)
        {
            var state = _states[i];
            if (state.Data == null || state.Data.Length == 0) continue;

            if (!state.IsFinished) allFinished = false;

            double cw = _canvases[i].ActualWidth;
            double ch = _canvases[i].ActualHeight;
            if (cw == 0 || ch == 0) continue;

            double barW = cw / state.Data.Length;

            for (int j = 0; j < state.Data.Length; j++)
            {
                int val = state.Data[j];
                double barH = ((double)val / state.MaxVal) * (ch - 2);

                var rect = _rectangles[i][j];
                rect.Width = Math.Max(1, barW - (barW > 3 ? 1 : 0));
                rect.Height = barH;
                Canvas.SetLeft(rect, j * barW);
                Canvas.SetTop(rect, ch - barH);

                if (state.IsFinished) rect.Fill = _greenBrush;
                else if (j == state.Active1) rect.Fill = _redBrush;
                else if (j == state.Active2) rect.Fill = _blueBrush;
                else if (state.SortedBegin > 0 && j < state.SortedBegin) rect.Fill = _greenBrush;
                else if (state.SortedEnd < state.Data.Length && j >= state.SortedEnd) rect.Fill = _greenBrush;
                else rect.Fill = _barBrush;
            }
        }

        if (allFinished)
        {
            _renderTimer.Stop();
            StartButton.IsEnabled = true;
            StopButton.IsEnabled = false;
        }
    }

    private void StopButton_Click(object sender, RoutedEventArgs e)
    {
        _cts?.Cancel();
        _renderTimer?.Stop();
        StartButton.IsEnabled = true;
        StopButton.IsEnabled = false;
    }

    private async Task RunSorts(CancellationToken token)
    {
        var tasks = new Task[9]
        {
            Task.Run(() => SelectionSort(_states[0], token), token),
            Task.Run(() => ShellSort(_states[1], token), token),
            Task.Run(() => InsertionSort(_states[2], token), token),
            Task.Run(() => MergeSort(_states[3], token), token),
            Task.Run(() => QuickSort(_states[4], token), token),
            Task.Run(() => HeapSort(_states[5], token), token),
            Task.Run(() => BubbleSort(_states[6], token), token),
            Task.Run(() => CombSort(_states[7], token), token),
            Task.Run(() => CocktailSort(_states[8], token), token)
        };

        try { await Task.WhenAll(tasks); } catch { }
    }

    private void SleepC(CancellationToken token) { token.WaitHandle.WaitOne(4); token.ThrowIfCancellationRequested(); }
    private void MarkC(VisualizerState s, int a, int b, CancellationToken token) { s.Active1 = a; s.Active2 = b; SleepC(token); }
    private void DoSwap(VisualizerState s, ref int a, ref int b, int i, int j, CancellationToken token)
    {
        int temp = a; a = b; b = temp;
        s.Active1 = i; s.Active2 = j;
        SleepC(token);
    }
    private void DoSet(VisualizerState s, int i, int val, CancellationToken token)
    {
        s.Data[i] = val;
        s.Active1 = i; s.Active2 = -1;
        SleepC(token);
    }

    private void SelectionSort(VisualizerState s, CancellationToken token)
    {
        try
        {
            int n = s.Data.Length;
            for (int i = 0; i < n - 1; i++)
            {
                int minIdx = i;
                for (int j = i + 1; j < n; j++)
                {
                    MarkC(s, minIdx, j, token);
                    if (s.Data[j] < s.Data[minIdx]) minIdx = j;
                }
                if (minIdx != i) DoSwap(s, ref s.Data[i], ref s.Data[minIdx], i, minIdx, token);
                s.SortedBegin = i + 1;
            }
            s.SortedBegin = n;
        }
        finally { FinishState(s); }
    }

    private void ShellSort(VisualizerState s, CancellationToken token)
    {
        try
        {
            int n = s.Data.Length;
            for (int gap = n / 2; gap > 0; gap /= 2)
            {
                for (int i = gap; i < n; i++)
                {
                    int temp = s.Data[i];
                    int j = i;
                    while (j >= gap)
                    {
                        MarkC(s, j, j - gap, token);
                        if (s.Data[j - gap] > temp)
                        {
                            DoSet(s, j, s.Data[j - gap], token);
                            j -= gap;
                        }
                        else break;
                    }
                    if (j != i) DoSet(s, j, temp, token);
                }
            }
            s.SortedBegin = n;
        }
        finally { FinishState(s); }
    }

    private void InsertionSort(VisualizerState s, CancellationToken token)
    {
        try
        {
            int n = s.Data.Length;
            for (int i = 1; i < n; i++)
            {
                int key = s.Data[i];
                int j = i - 1;
                while (j >= 0)
                {
                    MarkC(s, j, i, token);
                    if (s.Data[j] > key)
                    {
                        DoSet(s, j + 1, s.Data[j], token);
                        j--;
                    }
                    else break;
                }
                if (j + 1 != i) DoSet(s, j + 1, key, token);
                s.SortedBegin = i + 1;
            }
            s.SortedBegin = n;
        }
        finally { FinishState(s); }
    }

    private void MergeSort(VisualizerState s, CancellationToken token)
    {
        try
        {
            void DoMerge(int l, int r)
            {
                if (l < r)
                {
                    int m = l + (r - l) / 2;
                    DoMerge(l, m);
                    DoMerge(m + 1, r);
                    token.ThrowIfCancellationRequested();
                    
                    int[] temp = new int[r - l + 1];
                    int i = l, j = m + 1, k = 0;
                    while (i <= m && j <= r)
                    {
                        MarkC(s, i, j, token);
                        if (s.Data[i] <= s.Data[j]) temp[k++] = s.Data[i++];
                        else temp[k++] = s.Data[j++];
                    }
                    while (i <= m) { temp[k++] = s.Data[i++]; SleepC(token); }
                    while (j <= r) { temp[k++] = s.Data[j++]; SleepC(token); }
                    
                    for (i = 0; i < temp.Length; i++) DoSet(s, l + i, temp[i], token);
                }
            }
            DoMerge(0, s.Data.Length - 1);
            s.SortedBegin = s.Data.Length;
        }
        finally { FinishState(s); }
    }

    private void QuickSort(VisualizerState s, CancellationToken token)
    {
        try
        {
            void DoQuick(int l, int r)
            {
                if (l >= r) return;
                int i = l, j = r;
                int pivot = s.Data[l + (r - l) / 2];
                while (i <= j)
                {
                    while (s.Data[i] < pivot) { MarkC(s, i, j, token); i++; }
                    while (s.Data[j] > pivot) { MarkC(s, i, j, token); j--; }
                    if (i <= j)
                    {
                        if (i < j) DoSwap(s, ref s.Data[i], ref s.Data[j], i, j, token);
                        i++; j--;
                    }
                }
                if (l < j) DoQuick(l, j);
                if (i < r) DoQuick(i, r);
            }
            DoQuick(0, s.Data.Length - 1);
            s.SortedBegin = s.Data.Length;
        }
        finally { FinishState(s); }
    }

    private void HeapSort(VisualizerState s, CancellationToken token)
    {
        try
        {
            int n = s.Data.Length;
            void SiftDown(int start, int end)
            {
                int root = start;
                while (root * 2 + 1 <= end)
                {
                    int child = root * 2 + 1;
                    int swap = root;
                    MarkC(s, swap, child, token);
                    if (s.Data[swap] < s.Data[child]) swap = child;
                    if (child + 1 <= end && s.Data[swap] < s.Data[child + 1])
                    {
                        MarkC(s, swap, child + 1, token);
                        swap = child + 1;
                    }
                    if (swap == root) return;
                    DoSwap(s, ref s.Data[root], ref s.Data[swap], root, swap, token);
                    root = swap;
                }
            }

            for (int i = (n - 2) / 2; i >= 0; i--) SiftDown(i, n - 1);
            for (int i = n - 1; i > 0; i--)
            {
                DoSwap(s, ref s.Data[0], ref s.Data[i], 0, i, token);
                s.SortedEnd = i;
                SiftDown(0, i - 1);
            }
            s.SortedEnd = 0;
            s.SortedBegin = n;
        }
        finally { FinishState(s); }
    }

    private void BubbleSort(VisualizerState s, CancellationToken token)
    {
        try
        {
            int n = s.Data.Length;
            for (int i = 0; i < n - 1; i++)
            {
                bool swapped = false;
                for (int j = 0; j < n - i - 1; j++)
                {
                    MarkC(s, j, j + 1, token);
                    if (s.Data[j] > s.Data[j + 1])
                    {
                        DoSwap(s, ref s.Data[j], ref s.Data[j + 1], j, j + 1, token);
                        swapped = true;
                    }
                }
                s.SortedEnd = n - 1 - i;
                if (!swapped) break;
            }
            s.SortedBegin = n;
        }
        finally { FinishState(s); }
    }

    private void CombSort(VisualizerState s, CancellationToken token)
    {
        try
        {
            int n = s.Data.Length;
            double gap = n;
            bool swapped = true;
            while (gap > 1 || swapped)
            {
                gap = Math.Max(1, Math.Floor(gap / 1.3));
                swapped = false;
                for (int i = 0; i + (int)gap < n; i++)
                {
                    int j = i + (int)gap;
                    MarkC(s, i, j, token);
                    if (s.Data[i] > s.Data[j])
                    {
                        DoSwap(s, ref s.Data[i], ref s.Data[j], i, j, token);
                        swapped = true;
                    }
                }
            }
            s.SortedBegin = n;
        }
        finally { FinishState(s); }
    }

    private void CocktailSort(VisualizerState s, CancellationToken token)
    {
        try
        {
            int n = s.Data.Length;
            bool swapped = true;
            int start = 0, end = n - 1;
            while (swapped)
            {
                swapped = false;
                for (int i = start; i < end; i++)
                {
                    MarkC(s, i, i + 1, token);
                    if (s.Data[i] > s.Data[i + 1])
                    {
                        DoSwap(s, ref s.Data[i], ref s.Data[i + 1], i, i + 1, token);
                        swapped = true;
                    }
                }
                if (!swapped) break;
                swapped = false;
                end--;
                s.SortedEnd = end + 1;

                for (int i = end - 1; i >= start; i--)
                {
                    MarkC(s, i, i + 1, token);
                    if (s.Data[i] > s.Data[i + 1])
                    {
                        DoSwap(s, ref s.Data[i], ref s.Data[i + 1], i, i + 1, token);
                        swapped = true;
                    }
                }
                start++;
                s.SortedBegin = start;
            }
            s.SortedBegin = n;
        }
        finally { FinishState(s); }
    }

    private void FinishState(VisualizerState s)
    {
        s.Active1 = -1;
        s.Active2 = -1;
        s.IsFinished = true;
    }
}
