// Copyright (c) Microsoft. All rights reserved.

import React from "react";

import { svgs } from "utilities";
import { Svg } from "components/shared/svg/svg";

const classnames = require("classnames/bind");
const css = classnames.bind(require("../cellRenderer.module.scss"));

export const GlimmerRenderer = (props) =>
    props.value ? (
        <Svg src={svgs.glimmer} className={css("glimmer-icon")} />
    ) : null;
