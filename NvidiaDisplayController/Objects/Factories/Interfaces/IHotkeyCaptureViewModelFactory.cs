using NvidiaDisplayController.Interface.HotkeyCapture;

namespace NvidiaDisplayController.Objects.Factories.Interfaces;

public interface IHotkeyCaptureViewModelFactory : IFactory
{
    HotkeyCaptureViewModel Create();
}
