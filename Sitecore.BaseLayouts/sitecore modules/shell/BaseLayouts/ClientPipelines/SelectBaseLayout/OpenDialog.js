﻿define(["myExperienceEditor"], function(experienceEditor) {
    return experienceEditor.PipelinesUtil.generateDialogCallProcessor({
        url: function(context) { return context.currentContext.value; },
        features: "dialogHeight: 600px;dialogWidth: 525px;",
        onSuccess: function(context, result) {
            context.currentContext.value = result;
        }
    });
});