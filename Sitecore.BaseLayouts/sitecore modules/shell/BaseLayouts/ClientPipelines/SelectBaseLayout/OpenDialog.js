﻿define(["/-/speak/v1/ExperienceEditor/ExperienceEditor.js"], function(ExperienceEditor) {
    return ExperienceEditor.PipelinesUtil.generateDialogCallProcessor({
        url: function(context) { return context.currentContext.value; },
        features: "dialogHeight: 600px;dialogWidth: 525px;",
        onSuccess: function(context, result) {
            context.currentContext.value = result;
        }
    });
});