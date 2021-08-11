// Copyright (c) Microsoft. All rights reserved.

import React from "react";

import { toDiagnosticsModel } from "services/models";
import { toDeviceConditionModel } from "services/models/configModels.js";
import { svgs, LinkedComponent, Validator } from "utilities";
import {
    AjaxError,
    Btn,
    BtnToolbar,
    FormControl,
    PropertyRow as Row,
    PropertyCell as Cell,
    PropertyGrid as Grid,
} from "components/shared";
import { defaultDownloadMappings } from "../devicesGrid/devicesGridConfig";

import { IoTHubManagerService } from "services";

const classnames = require("classnames/bind");
const css = classnames.bind(require("./advanceSearch.module.scss"));

// A counter for creating unique keys per new condition
let conditionKey = 0;

const operators = ["EQ", "GT", "LT", "GE", "LE", "LK"],
    // Creates a state object for a condition
    newCondition = () => ({
        field: undefined,
        operator: undefined,
        value: "",
        key: conditionKey++, // Used by react to track the rendered elements
    });

export class AdvanceSearch extends LinkedComponent {
    constructor(props) {
        super(props);

        this.state = {
            filterOptions: [
                { label: "Device Name", value: "deviceId" },
                {
                    label: "Firmware",
                    value: "firmwareVersion",
                },
                { label: "Status", value: "connectionState" },
            ],
            statusOptions: [
                { label: "Online", value: "Connected" },
                { label: "Offline", value: "Disconnected" },
            ],
            deviceQueryConditions: [],
            isPending: false,
            error: undefined,
            enableDownload: false,
        };

        // State to input links
        this.conditionsLink = this.linkTo("deviceQueryConditions");
        this.subscriptions = [];
    }

    conditionIsNew(condition) {
        return !condition.field && !condition.operator && !condition.value;
    }

    formIsValid() {
        return [this.conditionsLink].every((link) => !link.error);
    }

    componentWillUnmount() {
        this.subscriptions.forEach((sub) => sub.unsubscribe());
    }

    queryDevices = () => {
        return new Promise((resolve, reject) => {
            try {
                this.setState({ error: undefined, isPending: true }, () => {
                    const rawQueryConditions = this.state.deviceQueryConditions.filter(
                        (condition) => {
                            // remove conditions that are new (have not been edited)
                            return !this.conditionIsNew(condition);
                        }
                    );
                    this.props.fetchDevicesByCondition({
                        data: rawQueryConditions.map((condition) => {
                            return toDeviceConditionModel(condition);
                        }),
                    });
                    resolve();
                });
            } catch (error) {
                reject(error);
            }
        });
    };

    apply = (event) => {
        this.props.logEvent(toDiagnosticsModel("CreateDeviceQuery_Create", {}));
        event.preventDefault();
        this.setState({ enableDownload: true });
        this.queryDevices()
            .then(() => {
                this.setState({ error: undefined, isPending: false });
            })
            .catch((error) => {
                this.setState({ error: error, isPending: false });
            });
    };

    addCondition = () => {
        this.props.logEvent(
            toDiagnosticsModel("CreateDeviceQuery_AddCondition", {})
        );
        return this.conditionsLink.set([
            ...this.conditionsLink.value,
            newCondition(),
        ]);
    };

    deleteCondition = (index) => () => {
        this.props.logEvent(
            toDiagnosticsModel("CreateDeviceQuery_RemoveCondition", {})
        );
        return this.conditionsLink.set(
            this.conditionsLink.value.filter((_, idx) => index !== idx)
        );
    };

    resetDeviceCondition = () => {
        this.setState(
            {
                deviceQueryConditions: [newCondition()],
                error: undefined,
            },
            () => {
                this.render();
            }
        );
    };

    onReset = () => {
        this.props.logEvent(toDiagnosticsModel("CreateDeviceQuery_Reset", {}));
        this.props.resetDeviceByCondition();
        this.setState({ enableDownload: false });
        this.resetDeviceCondition();
    };

    operatorOptionArr = (options, key) => {
        var optionArr = options;
        if (
            this.state.deviceQueryConditions.length > 0 &&
            this.state.deviceQueryConditions[key].field !== "deviceId"
        ) {
            optionArr = optionArr.filter((option) => option.value !== "LK");
            if (this.state.deviceQueryConditions[key].operator === "LK") {
                this.state.deviceQueryConditions[key].operator = undefined;
            }
        }

        return optionArr;
    };

    onFieldChange = (key) => {
        if (
            this.state.deviceQueryConditions[key].field !== "connectionState" &&
            (this.state.deviceQueryConditions[key].value === "Connected" ||
                this.state.deviceQueryConditions[key].value === "Disconnected")
        ) {
            this.state.deviceQueryConditions[key].value = "";
        }
    };

    downloadFile = () => {
        const rawQueryConditions = this.state.deviceQueryConditions.filter(
            (condition) => {
                return !this.conditionIsNew(condition);
            }
        );

        IoTHubManagerService.getDevicesReportByQuery(
            rawQueryConditions.map((condition) => {
                return toDeviceConditionModel(condition);
            }),
            defaultDownloadMappings
        ).subscribe((response) => {
            var blob = new Blob([response.response], {
                type: response.response.type,
            });
            let url = window.URL.createObjectURL(blob);
            let a = document.createElement("a");
            a.href = url;
            a.download = "FilteredDevicesList.xlsx";
            a.click();
        });
    };

    render() {
        const { t } = this.props,
            // Create the state link for the dynamic form elements
            conditionLinks = this.conditionsLink.getLinkedChildren(
                (conditionLink) => {
                    let field = conditionLink
                            .forkTo("field")
                            .map(({ value }) => value)
                            .check(
                                Validator.notEmpty,
                                t(
                                    "deviceQueryConditions.errorMsg.fieldCantBeEmpty"
                                )
                            ),
                        operator = conditionLink
                            .forkTo("operator")
                            .map(({ value }) => value)
                            .check(
                                Validator.notEmpty,
                                t(
                                    "deviceQueryConditions.errorMsg.operatorCantBeEmpty"
                                )
                            ),
                        value = conditionLink
                            .forkTo("value")
                            .check(
                                Validator.notEmpty,
                                t(
                                    "deviceQueryConditions.errorMsg.valueCantBeEmpty"
                                )
                            ),
                        edited = !(
                            !field.value &&
                            !operator.value &&
                            !value.value
                        );
                    if (field.value === "connectionState") {
                        value = conditionLink
                            .forkTo("value")
                            .map(({ value }) => value)
                            .check(
                                Validator.notEmpty,
                                t(
                                    "deviceQueryConditions.errorMsg.valueCantBeEmpty"
                                )
                            );
                    }
                    let error =
                        field.error || operator.error || value.error || "";
                    return { field, operator, value, edited, error };
                }
            ),
            conditionHasErrors = conditionLinks.some(({ error }) => !!error),
            operatorOptions = operators.map((value) => ({
                label: t(`deviceQueryConditions.operatorOptions.${value}`),
                value,
            }));

        return (
            <form onSubmit={this.apply}>
                <div className={css("manage-filters-container")}>
                    <Grid>
                        {conditionLinks.length > 0 && (
                            <Row>
                                <Cell className="col-1"></Cell>
                                <Cell className="col-1"></Cell>
                                <Cell className="col-3">Field</Cell>
                                <Cell className="col-1"></Cell>
                                <Cell className="col-2">Operator</Cell>
                                <Cell className="col-1"></Cell>
                                <Cell className="col-3">Value</Cell>
                            </Row>
                        )}
                        {conditionLinks.map((condition, idx) => (
                            <Row
                                key={this.state.deviceQueryConditions[idx].key}
                                // className="deviceExplorer-conditions"
                            >
                                <Cell className="col-1">
                                    <Btn
                                        className={css("btn-icon")}
                                        svg={svgs.plus}
                                        onClick={this.addCondition}
                                    />
                                </Cell>
                                <Cell className="col-1">
                                    <Btn
                                        className="btn-icon"
                                        icon="cancel"
                                        onClick={this.deleteCondition(idx)}
                                    />
                                </Cell>
                                <Cell className="col-3">
                                    <FormControl
                                        type="select"
                                        ariaLabel={t(
                                            "deviceQueryConditions.field"
                                        )}
                                        className="long"
                                        searchable={false}
                                        clearable={false}
                                        placeholder={t(
                                            "deviceQueryConditions.fieldPlaceholder"
                                        )}
                                        options={this.state.filterOptions}
                                        link={condition.field}
                                        onChange={this.onFieldChange(idx)}
                                    />
                                </Cell>
                                <Cell className="col-1"></Cell>
                                <Cell className="col-2">
                                    <FormControl
                                        type="select"
                                        ariaLabel={t(
                                            "deviceQueryConditions.operator"
                                        )}
                                        className="long"
                                        searchable={false}
                                        clearable={false}
                                        options={this.operatorOptionArr(
                                            operatorOptions,
                                            idx
                                        )}
                                        placeholder={t(
                                            "deviceQueryConditions.operatorPlaceholder"
                                        )}
                                        link={condition.operator}
                                    />
                                </Cell>
                                <Cell className="col-1"></Cell>
                                <Cell className="col-3">
                                    {this.state.deviceQueryConditions[idx]
                                        .field !== "connectionState" && (
                                        <FormControl
                                            type="text"
                                            placeholder={t(
                                                "deviceQueryConditions.valuePlaceholder"
                                            )}
                                            link={condition.value}
                                            className={css("width-70")}
                                        />
                                    )}
                                    {this.state.deviceQueryConditions[idx]
                                        .field === "connectionState" && (
                                        <FormControl
                                            type="select"
                                            ariaLabel={t(
                                                "deviceQueryConditions.status"
                                            )}
                                            className="long"
                                            searchable={false}
                                            clearable={false}
                                            options={this.state.statusOptions}
                                            placeholder={t(
                                                "deviceQueryConditions.statusPlaceholder"
                                            )}
                                            link={condition.value}
                                        />
                                    )}
                                </Cell>
                            </Row>
                        ))}
                    </Grid>
                    <Btn
                        className={css("add-btn")}
                        svg={svgs.plus}
                        onClick={this.addCondition}
                    >
                        Add a condition
                    </Btn>
                    <div className={css("cancel-right-div")}>
                        <BtnToolbar>
                            <Btn
                                svg={svgs.upload}
                                className={css("download-deviceQueryReport")}
                                disabled={
                                    !this.state.enableDownload ||
                                    !this.formIsValid() ||
                                    conditionHasErrors ||
                                    this.state.isPending ||
                                    this.state.deviceQueryConditions.length ===
                                        0
                                }
                                onClick={this.downloadFile}
                            >
                                {t("devices.downloadDeviceReport")}
                            </Btn>
                            <Btn
                                primary
                                disabled={
                                    !this.formIsValid() ||
                                    conditionHasErrors ||
                                    this.state.isPending ||
                                    this.state.deviceQueryConditions.length ===
                                        0
                                }
                                type="submit"
                            >
                                Query
                            </Btn>
                            <Btn
                                disabled={this.state.isPending}
                                svg={svgs.cancelX}
                                onClick={this.onReset}
                            >
                                Reset
                            </Btn>
                        </BtnToolbar>
                    </div>
                    {this.state.error && (
                        <AjaxError t={t} error={this.state.error} />
                    )}
                </div>
            </form>
        );
    }
}
