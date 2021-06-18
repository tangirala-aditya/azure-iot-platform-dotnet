// Copyright (c) Microsoft. All rights reserved.

import React, { Fragment } from "react";

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

const classnames = require("classnames/bind");
const css = classnames.bind(require("./columnMapping.scss"));

// A counter for creating unique keys per new condition
let conditionKey = 0;

const newColumnMapping = () => ({
    name: undefined,
    mapping: undefined,
    description: "",
    key: conditionKey++, // Used by react to track the rendered elements
});

export class ColumnMapper extends LinkedComponent {
    constructor(props) {
        super(props);
        this.state = {
            mappingOptions: [
                {
                    label: "Id",
                    value: "id",
                },
                {
                    label: "lastActivity",
                    value: "lastActivity",
                },
                {
                    label: "isSimulated",
                    value: "isSimulated",
                },
                {
                    label: "c2DMessageCount",
                    value: "c2DMessageCount",
                },
                {
                    label: "enabled",
                    value: "enabled",
                },
                {
                    label: "lastStatusUpdated",
                    value: "lastStatusUpdated",
                },
                {
                    label: "iotHubHostName",
                    value: "iotHubHostName",
                },
                {
                    label: "eTag",
                    value: "eTag",
                },
                {
                    label: "authentication",
                    value: "authentication",
                },
            ],
            statusOptions: [
                { label: "Online", value: "Connected" },
                { label: "Offline", value: "Disconnected" },
            ],
            mappingName: this.props.isEdit
                ? this.props.match.params.name
                : this.props.isDefault
                ? "Default"
                : "",
            isEdit: this.props.isEdit || false,
            isDefault: this.props.isDefault || false,
            columnMappings: this.props.isEdit
                ? (
                      this.props.columnMappings[this.props.match.params.name] ||
                      {}
                  ).mapping || []
                : this.props.isDefault
                ? (this.props.columnMappings["Default"] || {}).mapping || []
                : [],
            defaultColumnMappings:
                (this.props.columnMappings["Default"] || {}).mapping || [],
            isPending: false,
            error: undefined,
        };

        // State to input links
        this.mappingsLink = this.linkTo("columnMappings");
        this.defaultMappingsLink = this.linkTo("defaultColumnMappings");
        this.mappingNameLink = this.linkTo("mappingName");
        this.subscriptions = [];
    }

    conditionIsNew(condition) {
        return !condition.field && !condition.operator && !condition.value;
    }

    formIsValid() {
        return [this.mappingsLink].every((link) => !link.error);
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
        event.preventDefault();
        console.log(this.state.columnMappings);
        if (this.state.isDefault) {
            this.props.history.push(`/columnMapping/default`);
        } else {
            this.props.history.push(`/columnMapping/custom`);
        }
    };

    addCondition = () => {
        this.props.logEvent(
            toDiagnosticsModel("CreateColumnMapping_AddColumnMapping", {})
        );
        return this.mappingsLink.set([
            ...this.mappingsLink.value,
            newColumnMapping(),
        ]);
    };

    deleteCondition = (index) => () => {
        this.props.logEvent(
            toDiagnosticsModel("CreateColumnMapping_DeleteColumnMapping", {})
        );
        return this.mappingsLink.set(
            this.mappingsLink.value.filter((_, idx) => index !== idx)
        );
    };

    resetDeviceCondition = () => {
        this.setState(
            {
                deviceQueryConditions: [newColumnMapping()],
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
        // if (
        //     this.state.deviceQueryConditions[key].field !== "connectionState" &&
        //     (this.state.deviceQueryConditions[key].value === "Connected" ||
        //         this.state.deviceQueryConditions[key].value === "Disconnected")
        // ) {
        //     this.state.deviceQueryConditions[key].value = "";
        // }
    };

    render() {
        const { t } = this.props,
            // Create the state link for the dynamic form elements
            mappingsLink = this.mappingsLink.getLinkedChildren(
                (conditionLink) => {
                    let name = conditionLink
                            .forkTo("name")
                            .check(
                                Validator.notEmpty,
                                t(
                                    "deviceQueryConditions.errorMsg.fieldCantBeEmpty"
                                )
                            ),
                        mapping = conditionLink
                            .forkTo("mapping")
                            .map(({ value }) => value)
                            .check(
                                Validator.notEmpty,
                                t(
                                    "deviceQueryConditions.errorMsg.operatorCantBeEmpty"
                                )
                            ),
                        description = conditionLink.forkTo("description"),
                        edited = !(
                            !name.value &&
                            !mapping.value &&
                            !description.value
                        );
                    let error =
                        name.error || mapping.error || description.error || "";
                    return { name, mapping, description, edited, error };
                }
            ),
            defaultMappingsLink = this.defaultMappingsLink.getLinkedChildren(
                (conditionLink) => {
                    let name = conditionLink
                            .forkTo("name")
                            .check(
                                Validator.notEmpty,
                                t(
                                    "deviceQueryConditions.errorMsg.fieldCantBeEmpty"
                                )
                            ),
                        mapping = conditionLink
                            .forkTo("mapping")
                            .map(({ value }) => value)
                            .check(
                                Validator.notEmpty,
                                t(
                                    "deviceQueryConditions.errorMsg.operatorCantBeEmpty"
                                )
                            ),
                        description = conditionLink.forkTo("description"),
                        edited = !(
                            !name.value &&
                            !mapping.value &&
                            !description.value
                        );
                    let error =
                        name.error || mapping.error || description.error || "";
                    return { name, mapping, description, edited, error };
                }
            ),
            conditionHasErrors = mappingsLink.some(({ error }) => !!error);

        return (
            <Fragment>
                {!(this.state.isDefault || this.state.isEdit) && (
                    <FormControl
                        type="text"
                        ariaLabel={t("deviceQueryConditions.field")}
                        className="long"
                        searchable={false}
                        clearable={false}
                        placeholder={t(
                            "deviceQueryConditions.fieldPlaceholder"
                        )}
                        link={this.mappingsLink.name}
                    />
                )}
                {!this.state.isDefault && this.state.isEdit && (
                    <p>{this.state.mappingName}</p>
                )}
                <form onSubmit={this.apply}>
                    <div className={css("manage-filters-container")}>
                        <Grid>
                            {mappingsLink.length > 0 && (
                                <Row>
                                    <Cell className="col-1"></Cell>
                                    <Cell className="col-1"></Cell>
                                    <Cell className="col-3">Name</Cell>
                                    <Cell className="col-1"></Cell>
                                    <Cell className="col-2">Mapping</Cell>
                                    <Cell className="col-1"></Cell>
                                    <Cell className="col-3">Description</Cell>
                                </Row>
                            )}
                            {!this.state.isDefault &&
                                defaultMappingsLink.map((condition, idx) => (
                                    <Row
                                        key={
                                            this.state.defaultColumnMappings[
                                                idx
                                            ].key
                                        }
                                        // className="deviceExplorer-conditions"
                                    >
                                        <Cell className="col-1"></Cell>
                                        <Cell className="col-1"></Cell>
                                        <Cell className="col-3">
                                            <FormControl
                                                type="text"
                                                ariaLabel={t(
                                                    "deviceQueryConditions.field"
                                                )}
                                                className="long"
                                                disabled={true}
                                                searchable={false}
                                                clearable={false}
                                                placeholder={t(
                                                    "deviceQueryConditions.fieldPlaceholder"
                                                )}
                                                link={condition.name}
                                                onChange={this.onFieldChange(
                                                    idx
                                                )}
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
                                                disabled={true}
                                                searchable={false}
                                                clearable={false}
                                                options={
                                                    this.state.mappingOptions
                                                }
                                                placeholder={t(
                                                    "deviceQueryConditions.operatorPlaceholder"
                                                )}
                                                link={condition.mapping}
                                            />
                                        </Cell>
                                        <Cell className="col-1"></Cell>
                                        <Cell className="col-3">
                                            <FormControl
                                                type="text"
                                                placeholder={t(
                                                    "deviceQueryConditions.valuePlaceholder"
                                                )}
                                                link={condition.description}
                                                className={css("width-70")}
                                            />
                                        </Cell>
                                    </Row>
                                ))}
                            {mappingsLink.map((condition, idx) => (
                                <Row
                                    key={this.state.columnMappings[idx].key}
                                    // className="deviceExplorer-conditions"
                                >
                                    {mappingsLink.length - 1 === idx && (
                                        <Cell className="col-1">
                                            <Btn
                                                className={css("btn-icon")}
                                                svg={svgs.plus}
                                                onClick={this.addCondition}
                                            />
                                        </Cell>
                                    )}
                                    {mappingsLink.length - 1 !== idx && (
                                        <Cell className="col-1"></Cell>
                                    )}
                                    <Cell className="col-1">
                                        <Btn
                                            className="btn-icon"
                                            icon="cancel"
                                            onClick={this.deleteCondition(idx)}
                                        />
                                    </Cell>
                                    <Cell className="col-3">
                                        <FormControl
                                            type="text"
                                            ariaLabel={t(
                                                "deviceQueryConditions.field"
                                            )}
                                            className="long"
                                            searchable={false}
                                            clearable={false}
                                            placeholder={t(
                                                "deviceQueryConditions.fieldPlaceholder"
                                            )}
                                            link={condition.name}
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
                                            options={this.state.mappingOptions}
                                            placeholder={t(
                                                "deviceQueryConditions.operatorPlaceholder"
                                            )}
                                            link={condition.mapping}
                                        />
                                    </Cell>
                                    <Cell className="col-1"></Cell>
                                    <Cell className="col-3">
                                        <FormControl
                                            type="text"
                                            placeholder={t(
                                                "deviceQueryConditions.valuePlaceholder"
                                            )}
                                            link={condition.description}
                                            className={css("width-70")}
                                        />
                                    </Cell>
                                </Row>
                            ))}
                        </Grid>
                        <Btn
                            className={css("add-btn")}
                            svg={svgs.plus}
                            onClick={this.addCondition}
                        >
                            Add a Mapping
                        </Btn>
                        <div className={css("cancel-right-div")}>
                            <BtnToolbar>
                                <Btn
                                    primary
                                    disabled={
                                        !this.formIsValid() ||
                                        conditionHasErrors ||
                                        this.state.isPending ||
                                        this.state.columnMappings.length === 0
                                    }
                                    type="submit"
                                >
                                    Save
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
            </Fragment>
        );
    }
}
