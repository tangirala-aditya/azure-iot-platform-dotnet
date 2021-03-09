// Copyright (c) Microsoft. All rights reserved.

import React, { Component } from "react";

import {
    Btn,
    FormGroup,
    FormLabel,
    FormControl,
    Indicator,
    Svg,
    FileInput,
} from "components/shared";
import { svgs, isValidExtension } from "utilities";
import Flyout from "components/shared/flyout";
import Config from "app.config";
import { toDiagnosticsModel } from "services/models";

// import "./applicationSettings.scss";

const classnames = require("classnames/bind");
const css = classnames.bind(require("./applicationSettings.module.scss"));

const Section = Flyout.Section;

export class ApplicationSettings extends Component {
    constructor(props) {
        super(props);

        this.state = {
            currentLogo: this.props.logo,
            currentApplicationName: this.props.name,
            edit: false,
            previewLogo: this.props.logo,
            newLogoName: undefined,
            isDefaultLogo: this.props.isDefaultLogo,
            validating: false,
            isValidFile: false,
        };
    }

    renderSvgLogo = (logo) => <Svg src={logo} className={css("logo-svg")} />;

    onApplicationNameInputClick = () => {
        this.props.logEvent(toDiagnosticsModel("Settings_NameUpdated", {}));
    };

    renderUploadContainer = () => {
        const { t, applicationNameLink } = this.props,
            {
                isDefaultLogo,
                isValidFile,
                currentLogo,
                previewLogo,
                newLogoName,
                currentApplicationName,
            } = this.state,
            fileNameClass = isValidFile
                ? "file-name-valid"
                : "file-name-invalid";
        return (
            <div className={css("upload-logo-name-container")}>
                <div className={css("upload-logo-container")}>
                    <div className={css("image-preview")}>
                        {isDefaultLogo ? (
                            this.renderSvgLogo(currentLogo)
                        ) : (
                            <img
                                className={css("logo-img")}
                                src={previewLogo}
                                alt={t("applicationSettings.previewLogo")}
                            />
                        )}
                    </div>
                    <div className={css("replace-logo")}>
                        {t("applicationSettings.replaceLogo")}
                    </div>
                    <div className={css("upload-btn-container")}>
                        <FileInput
                            className={css("upload-button")}
                            onChange={this.onUpload}
                            accept={Config.validExtensions}
                            label={t("applicationSettings.upload")}
                            t={t}
                        />
                        <div className={css("file-upload-feedback")}>
                            {isValidFile ? (
                                <Svg
                                    className={css("checkmark")}
                                    src={svgs.checkmark}
                                    alt={t("applicationSettings.checkmark")}
                                />
                            ) : (
                                newLogoName && (
                                    <Svg
                                        className={css("invalid-file-x")}
                                        src={svgs.x}
                                        alt={t("applicationSettings.error")}
                                    />
                                )
                            )}
                        </div>
                        <div className={css(fileNameClass)}>{newLogoName}</div>
                    </div>
                    {!isValidFile && newLogoName && (
                        <div className={css("upload-error-message")}>
                            <Svg
                                className={css("upload-error-asterisk")}
                                src={svgs.error}
                                alt={t("applicationSettings.error")}
                            />
                            {t("applicationSettings.uploadError")}
                        </div>
                    )}
                    <Section.Content
                        className={css(
                            "platform-section-description",
                            "show-line-breaks"
                        )}
                    >
                        {t("applicationSettings.logoDescription")}
                    </Section.Content>
                </div>
                <FormGroup className={css("name-input-container")}>
                    <FormLabel className={css("section-subtitle")}>
                        {t("applicationSettings.applicationName")}
                    </FormLabel>
                    <FormControl
                        type="text"
                        className={css("name-input", "long")}
                        placeholder={t(currentApplicationName)}
                        link={applicationNameLink}
                        onClick={this.onApplicationNameInputClick}
                    />
                </FormGroup>
            </div>
        );
    };

    render() {
        const { t } = this.props,
            {
                isDefaultLogo,
                validating,
                currentLogo,
                currentApplicationName,
                edit,
            } = this.state;
        return (
            <Section.Container className={css("setting-section")}>
                <Section.Header>
                    {t("applicationSettings.nameAndLogo")}
                </Section.Header>
                <Section.Content>
                    {t("applicationSettings.nameLogoDescription")}
                </Section.Content>
                <Section.Content>
                    {edit ? (
                        validating ? (
                            <div className={css("upload-logo-name-container")}>
                                <Indicator size="small" />
                            </div>
                        ) : (
                            this.renderUploadContainer()
                        )
                    ) : (
                        <div>
                            <div className={css("current-logo-container")}>
                                <div className={css("current-logo-name")}>
                                    <div className={css("current-logo")}>
                                        {isDefaultLogo ? (
                                            this.renderSvgLogo(currentLogo)
                                        ) : (
                                            <img
                                                className={css("current-logo")}
                                                src={currentLogo}
                                                alt={t(
                                                    "applicationSettings.currentLogo"
                                                )}
                                            />
                                        )}
                                    </div>
                                    <div className={css("name-container")}>
                                        {t(currentApplicationName)}
                                    </div>
                                </div>
                                <div className={css("edit-button-div")}>
                                    <Btn
                                        type="button"
                                        svg={svgs.edit}
                                        onClick={this.enableEdit}
                                        className={css("edit-button")}
                                    >
                                        {t("applicationSettings.edit")}
                                    </Btn>
                                </div>
                            </div>
                        </div>
                    )}
                </Section.Content>
            </Section.Container>
        );
    }

    enableEdit = () => this.setState({ edit: true });

    onUpload = (e) => {
        let file = e.target.files[0];
        this.setState({
            validating: true,
            validFile: false,
        });
        if (
            file.size <= Config.maxLogoFileSizeInBytes &&
            isValidExtension(file)
        ) {
            this.setState({
                newLogoName: file.name,
                previewLogo: URL.createObjectURL(file),
                isDefaultLogo: false,
                validating: false,
                isValidFile: true,
            });
        } else {
            this.setState({
                previewLogo: this.state.currentLogo,
                newLogoName: file !== undefined ? file.name : undefined,
                isDefaultLogo: this.props.isDefaultLogo,
                validating: false,
                isValidFile: false,
            });
            file = undefined;
        }
        this.props.onUpload(file);
    };
}

export default ApplicationSettings;
