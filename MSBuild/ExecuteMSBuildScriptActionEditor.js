function BmExecuteMSBuildScriptActionEditor(o) {
    /// <param name="o" value="{
    /// ddlConfigId: '',
    /// divConfigId: '',
    /// ddlPlatformId: '',
    /// divPlatformId: '',
    /// ddlTargetDirId: '',
    /// divTargetDirId: '',
    /// chkWebProjectId: ''
    /// }"/>

    var bindToDropDown = function (ddl, target, value) {
        var onload = $(ddl).find('option').val();
        if (onload == value)
            $(target).show();

        $(ddl).change(function () {
            var selectedConfig = $(this).val();
            if (selectedConfig == value)
                $(target).show();
            else
                $(target).hide();
        });
    }

    bindToDropDown(o.ddlConfigId, o.divConfigId, 'Other');
    bindToDropDown(o.ddlPlatformId, o.divPlatformId, 'Other');
    bindToDropDown(o.ddlTargetDirId, o.divTargetDirId, 'target');

    $(o.chkWebProjectId).change(function () {
        if ($(this).is(':checked')) {
            $(o.ddlBuildOutputDirId).val('target').attr('disabled', 'disabled');
            $(o.divTargetDirId).show();
        }
        else
            $(o.ddlBuildOutputDirId).removeAttr('disabled');
    });
}