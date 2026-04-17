#pragma once

#include "Modules/ModuleManager.h"

class FLocalSTTEditorModule : public IModuleInterface
{
public:
    virtual void StartupModule() override;
    virtual void ShutdownModule() override;
};
