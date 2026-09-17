(setq *LT2LAYER-LAST* nil)

(defun GetRealLinetype (ent / ed lt lay layrec)
  (setq ed (entget ent))
  (setq lt (cdr (assoc 6 ed)))
  (if (or (null lt) (= lt "BYLAYER"))
    (progn
      (setq lay (cdr (assoc 8 ed)))
      (setq layrec (tblsearch "LAYER" lay))
      (cdr (assoc 6 layrec))
    )
    lt
  )
)

(defun GetLayerByNumber (/ layers lay i idx)
  (setq layers '())
  (setq lay (tblnext "LAYER" T))
  (while lay
    (setq layers (cons (cdr (assoc 2 lay)) layers))
    (setq lay (tblnext "LAYER"))
  )
  (setq layers (acad_strlsort layers))

  (princ "\n================ LAYER LIST ================")
  (setq i 0)
  (foreach l layers
    (princ (strcat "\n[" (itoa i) "]  " l))
    (setq i (1+ i))
  )
  (princ "\n============================================")

  (if *LT2LAYER-LAST*
    (princ (strcat "\n<Enter = dùng lại layer: " *LT2LAYER-LAST* ">"))
  )

  (setq idx (getint "\nNhập SỐ Layer đích: "))

  (cond
    ((null idx)
     *LAYERCHANGEAM-LAST*
    )
    ((and (>= idx 0) (< idx (length layers)))
     (setq *LT2LAYER-LAST* (nth idx layers))
    )
    (T
     (princ "\n❌ Số không hợp lệ.")
     nil
    )
  )
)

(defun c:LAYERCHANGEAM (/ entSample ltSample ss i ent layName)

  (vl-load-com)

  ;; 1. Chọn object mẫu
  (setq entSample (car (entsel "\nChọn đối tượng mẫu (lấy Linetype): ")))
  (if (not entSample) (exit))

  (setq ltSample (GetRealLinetype entSample))
  (princ (strcat "\nLinetype mẫu: " ltSample))

  ;; 2. Quét chọn mặc định CAD (có highlight)
  (princ "\nQuét chọn vùng (Window / Crossing / Fence / CP...)")
  (setq ss (ssget))
  (if (not ss) (exit))

  ;; 3. Chọn layer đích
  (setq layName (GetLayerByNumber))
  (if (not layName) (exit))

  ;; 4. Chuyển layer
  (setq i 0)
  (repeat (sslength ss)
    (setq ent (ssname ss i))
    (if (= (GetRealLinetype ent) ltSample)
      (entmod
        (subst
          (cons 8 layName)
          (assoc 8 (entget ent))
          (entget ent)
        )
      )
    )
    (setq i (1+ i))
  )

  (princ
    (strcat
      "\n✔ Hoàn thành – Layer hiện tại: "
      layName
    )
  )
  (princ)
)
