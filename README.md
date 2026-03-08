# C# (.NET 10) WinUI 3 Multi-threaded Sorting Algorithms Visualizer

## Overview
This is a modern reconstruction of a multi-threaded sorting algorithms visualizer using **C# 14 (.NET 10)** and Microsoft's **Windows App SDK (WinUI 3)**. It visually replicates parallel array processing across 9 simultaneous sorting algorithms using contemporary Task Asynchronous Programming (TAP) techniques.

The application dynamically scales a 3x3 grid matching the sorting arrays to an adaptive canvas, creating a fluid, flicker-free representation of real-time multi-core workloads. 

## Algorithms Visualized
1. Selection Sort
2. Shell Sort
3. Insertion Sort
4. Merge Sort
5. Quick Sort
6. Heap Sort
7. Bubble Sort
8. Comb Sort
9. Cocktail Shaker Sort

## C# / .NET 10 Architecture Showcased
- **Task Parallel Library (TPL):** Core workload executions are dispatched via `Task.Run()` and asynchronously orchestrated via `await Task.WhenAll()`, entirely preventing UI blockages and taking full advantage of modern multicore CPUs.
- **Graceful Thread Cancellation:** Employs `.NET` `CancellationTokenSource (_cts)`. A fully integrated **Stop** button allows users to securely abort 9 intense algorithmic background threads instantly and securely using `.ThrowIfCancellationRequested()`.
- **WinUI 3 Dynamic Canvas Engine:** Bypasses legacy UI databinding for high-performance array plotting directly mapped onto hardware-accelerated `Canvas` primitives with `Rectangle` shapes.
- **Decoupled Render Ticks:** Safe synchronization via a decoupled `DispatcherTimer` acting at ~30 FPS avoids the heavy penalty of cross-thread locks while keeping the red/blue algorithm pointers snappy.
- **Unpackaged Deployment Schema:** Configuration cleanly avoids complex MSIX app containers and can be locally executed via standard `.cs/exe` boundaries smoothly integrating into GitHub CI/CD build environments.

## How to Compile & Run
This project requires the .NET SDK (v8.0 or newer; optimally v10.0+ as configured).

1. Open your terminal or developer command prompt.
2. Navigate into the WinUI project directory:
   ```bash
   cd MultiSearchDemoWinUI
   ```
3. Run directly from the console:
   ```bash
   dotnet run
   ```
- You do not need to deal with Windows Appx/MSIX registry requirements or deploy scripts. It will natively generate an `.exe` applying complete Windows 11 Fluent theme styling schemas!
"# multi-threaded-sort-algorithms_C-_WinUI_XAML" 
